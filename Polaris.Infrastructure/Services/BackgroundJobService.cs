using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using Polaris.Application.Common.DTOs;
using Polaris.Application.Common.Interfaces;
using System.Linq.Expressions;

namespace Polaris.Infrastructure.Services
{
    /// <summary>
    /// Service wrapper around Hangfire for managing background jobs.
    /// Provides methods to enqueue, delete, retry, and inspect background jobs.
    /// 
    /// This abstraction decouples the Application layer from Hangfire's
    /// implementation details, following the Dependency Inversion Principle.
    /// </summary>
    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly IBackgroundJobClient _hangfire;
        private readonly ILogger<BackgroundJobService> _logger;

        public BackgroundJobService(
            IBackgroundJobClient hangfire,
            ILogger<BackgroundJobService> logger)
        {
            _hangfire = hangfire;
            _logger = logger;
        }

        /// <summary>
        /// Enqueues a new background job for immediate execution.
        /// Returns the Hangfire job ID for tracking purposes.
        /// </summary>
        public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
        {
            return _hangfire.Enqueue(methodCall);
        }

        /// <summary>
        /// Deletes a background job by its Hangfire job ID.
        /// Returns true if the job was successfully deleted.
        /// </summary>
        public bool Delete(string jobId)
        {
            return BackgroundJob.Delete(jobId);
        }

        /// <summary>
        /// Retrieves the current status of a Hangfire background job.
        /// 
        /// This method performs several lookups to gather comprehensive status info:
        /// 1. GetJobData — checks if the job exists and gets its current state
        /// 2. GetStateData — retrieves error messages from the current state
        /// 3. JobDetails (History) — falls back to job history for error details
        /// 
        /// Note: Hangfire's storage API is synchronous, so we wrap calls in Task.Run
        /// to avoid blocking the async pipeline.
        /// </summary>
        /// <param name="jobId">The Hangfire job ID to check.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A JobStatusInfo containing the job's existence, state, and any error messages.</returns>
        public async Task<JobStatusInfo> GetJobStatusAsync(
            string? jobId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId))
                {
                    return new JobStatusInfo { Exists = false };
                }

                _logger.LogDebug("Checking status for Hangfire job {JobId}", jobId);

                // Hangfire's storage API is synchronous, so we offload to the thread pool
                return await Task.Run(() =>
                {
                    using var connection = JobStorage.Current.GetConnection();

                    // Step 1: Check if the job exists at all
                    var jobData = connection.GetJobData(jobId);

                    if (jobData == null)
                    {
                        _logger.LogDebug("Hangfire job {JobId} not found", jobId);
                        return new JobStatusInfo { Exists = false };
                    }

                    // Step 2: Try to extract error information from the current state
                    string? error = null;
                    string? state = jobData.State;

                    try
                    {
                        var stateData = connection.GetStateData(jobId);
                        if (stateData != null)
                        {
                            // Attempt to extract error message from various Hangfire state data keys
                            stateData.Data?.TryGetValue("ExceptionMessage", out error);
                            if (string.IsNullOrEmpty(error))
                                stateData.Data?.TryGetValue("ErrorMessage", out error);
                            if (string.IsNullOrEmpty(error))
                                stateData.Data?.TryGetValue("ExceptionDetails", out error);

                            // If the state is "Failed" but no error message was found in data keys,
                            // fall back to the state's Reason field
                            if (string.IsNullOrEmpty(error) && stateData.Name == "Failed")
                            {
                                error = stateData.Reason ?? "Job failed without specific error message";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Could not get state data for job {JobId}", jobId);
                    }

                    // Step 3: If we still don't have an error message, check the job history.
                    // The history contains all state transitions, so we can find past failures.
                    try
                    {
                        var monitor = JobStorage.Current.GetMonitoringApi();
                        var jobDetails = monitor.JobDetails(jobId);

                        if (string.IsNullOrEmpty(error) && jobDetails?.History != null)
                        {
                            foreach (var historyItem in jobDetails.History)
                            {
                                if (historyItem.StateName == "Failed")
                                {
                                    historyItem.Data.TryGetValue("ExceptionMessage", out error);
                                    if (!string.IsNullOrEmpty(error)) break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Could not get job details for {JobId}", jobId);
                    }

                    _logger.LogDebug("Hangfire job {JobId} found with state {State}", jobId, state);

                    return new JobStatusInfo
                    {
                        Exists = true,
                        State = state,
                        CreatedAt = jobData.CreatedAt,
                        Error = error
                    };
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Hangfire job {JobId}", jobId);
                return new JobStatusInfo
                {
                    Exists = false,
                    Error = $"Error checking job: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Retrieves a list of currently active (processing/enqueued/scheduled) Hangfire jobs.
        /// </summary>
        public async Task<List<JobStatusInfo>> GetActiveJobsAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var monitor = JobStorage.Current.GetMonitoringApi();
                    var processingJobs = monitor.ProcessingJobs(0, int.MaxValue);
                    var scheduledJobs = monitor.ScheduledJobs(0, int.MaxValue);
                    var enqueuedJobs = monitor.EnqueuedJobs("default", 0, int.MaxValue);

                    var result = new List<JobStatusInfo>();

                    foreach (var job in processingJobs)
                    {
                        result.Add(new JobStatusInfo
                        {
                            Exists = true,
                            State = "Processing",
                            Error = null
                        });
                    }

                    return result;
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active jobs");
                return new List<JobStatusInfo>();
            }
        }

        /// <summary>
        /// Re-enqueues a failed job for another execution attempt.
        /// Returns true if the job was successfully re-queued.
        /// </summary>
        public bool Retry(string jobId)
        {
            try
            {
                return BackgroundJob.Requeue(jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying job {JobId}", jobId);
                return false;
            }
        }
    }
}