using Polaris.Application.Common.DTOs;
using System.Linq.Expressions;

namespace Polaris.Application.Common.Interfaces
{
    /// <summary>
    /// Abstraction for the background job scheduling service (Hangfire).
    /// Provides methods to enqueue, delete, and monitor background jobs
    /// without coupling the Application layer to Hangfire directly.
    /// </summary>
    public interface IBackgroundJobService
    {
        /// <summary>
        /// Enqueues a new background job for immediate execution.
        /// Returns the background job ID for tracking.
        /// </summary>
        string Enqueue<T>(Expression<Func<T, Task>> methodCall);

        /// <summary>
        /// Deletes a background job by its ID.
        /// Returns true if the job was successfully deleted.
        /// </summary>
        bool Delete(string jobId);

        /// <summary>
        /// Retrieves the current status of a background job, including
        /// its state, creation time, and any error messages.
        /// </summary>
        Task<JobStatusInfo> GetJobStatusAsync(
            string? jobId,
            CancellationToken cancellationToken = default);
    }
}
