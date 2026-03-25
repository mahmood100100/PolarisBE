using MediatR;
using Microsoft.Extensions.Logging;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Interfaces.IRepositories;
using System.Runtime.CompilerServices;

namespace Polaris.Application.Features.Conversations.Queries.StreamChatJobById
{
    public class StreamJobByIdQuery : IRequest<StreamJobByIdResult>
    {
        public Guid JobId { get; set; }
        public Guid UserId { get; set; }
    }

    public class StreamJobByIdResult
    {
        public bool Found { get; set; }
        public string? Error { get; set; }
        public IAsyncEnumerable<StreamEvent>? Events { get; set; }
    }

    public class StreamEvent
    {
        public StreamEventType Type { get; set; }
        public string? Content { get; set; }
        public string? Error { get; set; }
        public string? Status { get; set; }
        public int Progress { get; set; }
        public string? PartialContent { get; set; }
        public string? WaitingTime { get; set; }
        public bool IsRaw { get; set; }
        public string? RawValue { get; set; }
    }

    public enum StreamEventType
    {
        Buffer,
        Chunk,
        Error,
        Heartbeat,
        Done
    }

    public class StreamJobByIdHandler : IRequestHandler<StreamJobByIdQuery, StreamJobByIdResult>
    {
        private readonly IGenerationJobRepository _jobRepository;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly IGenerationStreamManager _streamManager;
        private readonly ILogger<StreamJobByIdHandler> _logger;

        public StreamJobByIdHandler(
            IGenerationJobRepository jobRepository,
            IBackgroundJobService backgroundJobService,
            IGenerationStreamManager streamManager,
            ILogger<StreamJobByIdHandler> logger)
        {
            _jobRepository = jobRepository;
            _backgroundJobService = backgroundJobService;
            _streamManager = streamManager;
            _logger = logger;
        }

        public async Task<StreamJobByIdResult> Handle(
            StreamJobByIdQuery request,
            CancellationToken cancellationToken)
        {
            var job = await _jobRepository.GetByIdAsync(request.JobId, cancellationToken);

            if (job == null || job.UserId != request.UserId)
            {
                return new StreamJobByIdResult { Found = false, Error = "Job not found" };
            }

            return new StreamJobByIdResult
            {
                Found = true,
                Events = ProduceEvents(request.JobId, request.UserId, cancellationToken)
            };
        }

        private async IAsyncEnumerable<StreamEvent> ProduceEvents(
            Guid jobId,
            Guid userId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);

            if (job == null || job.UserId != userId)
            {
                yield return new StreamEvent { Type = StreamEventType.Error, Error = "Job not found" };
                yield break;
            }

            var lastLength = 0;

            if (!string.IsNullOrEmpty(job.Result))
            {
                lastLength = job.Result.Length;
                yield return new StreamEvent
                {
                    Type = StreamEventType.Buffer,
                    Content = job.Result,
                    Status = job.Status,
                    Progress = job.Progress
                };
            }

            if (job.Status == "Completed")
            {
                yield return new StreamEvent { Type = StreamEventType.Done, IsRaw = true, RawValue = "[DONE]" };
                yield break;
            }
            if (job.Status == "Failed" || job.Status == "Cancelled")
            {
                yield return new StreamEvent
                {
                    Type = StreamEventType.Error,
                    Error = job.Error ?? $"Job {job.Status}",
                    Status = job.Status
                };
                yield break;
            }

            var waitAttempts = 0;
            while (!_streamManager.HasChannel(jobId) && waitAttempts < 25 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(200, cancellationToken);
                waitAttempts++;
            }

            if (_streamManager.HasChannel(jobId))
            {
                var dbBufferLength = (job.Result ?? string.Empty).Length;
                var skippedLength = 0;

                await foreach (var chunk in _streamManager.ReadAsync(jobId, cancellationToken))
                {
                    if (skippedLength < dbBufferLength)
                    {
                        var remainingDbLength = dbBufferLength - skippedLength;
                        if (chunk.Length <= remainingDbLength)
                        {
                            skippedLength += chunk.Length;
                            continue;
                        }
                        else
                        {
                            var newPart = chunk.Substring(remainingDbLength);
                            skippedLength = dbBufferLength;
                            if (!string.IsNullOrEmpty(newPart))
                            {
                                yield return new StreamEvent { Type = StreamEventType.Chunk, Content = newPart, Status = "Processing" };
                            }
                            continue;
                        }
                    }

                    yield return new StreamEvent { Type = StreamEventType.Chunk, Content = chunk, Status = "Processing" };
                }

                var finalJob = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
                if (finalJob?.Status == "Failed" || finalJob?.Status == "Cancelled")
                {
                    yield return new StreamEvent { Type = StreamEventType.Error, Error = finalJob.Error ?? "Error", Status = finalJob.Status };
                }
                else
                {
                    yield return new StreamEvent { Type = StreamEventType.Done, IsRaw = true, RawValue = "[DONE]" };
                }

                _streamManager.RemoveChannel(jobId);
            }
            else
            {
                await foreach (var fallbackEvent in FallbackDbPolling(jobId, userId, lastLength, cancellationToken))
                {
                    yield return fallbackEvent;
                }
            }
        }

        private async IAsyncEnumerable<StreamEvent> FallbackDbPolling(
            Guid jobId,
            Guid userId,
            int lastLength,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var noDataCount = 0;
            var startTime = DateTime.UtcNow;
            var lastDataTime = DateTime.UtcNow;

            while (!cancellationToken.IsCancellationRequested)
            {
                var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);

                if (job == null || job.UserId != userId)
                {
                    yield return new StreamEvent { Type = StreamEventType.Error, Error = "Job not found" };
                    yield break;
                }

                if (job.Result?.Length > lastLength)
                {
                    var newChunk = job.Result.Substring(lastLength);
                    lastLength = job.Result.Length;
                    noDataCount = 0;
                    lastDataTime = DateTime.UtcNow;

                    yield return new StreamEvent { Type = StreamEventType.Chunk, Content = newChunk, Status = job.Status, Progress = job.Progress };
                    continue;
                }
                else
                {
                    noDataCount++;
                }

                var isProcessing = job.Status == "Processing" || job.Status == "Pending";

                if (isProcessing && !string.IsNullOrEmpty(job.HangfireJobId))
                {
                    var hangfireStatus = await _backgroundJobService.GetJobStatusAsync(job.HangfireJobId, cancellationToken);
                    if (!hangfireStatus.Exists)
                    {
                        job.Status = "Failed";
                        job.Error = "Background process was terminated";
                        await _jobRepository.UpdateAsync(job, cancellationToken);
                        
                        yield return new StreamEvent { Type = StreamEventType.Error, Error = job.Error, Status = "Failed", PartialContent = job.Result };
                        yield break;
                    }

                    if (hangfireStatus.State == "Failed")
                    {
                        job.Status = "Failed";
                        job.Error = hangfireStatus.Error ?? "Unknown error";
                        await _jobRepository.UpdateAsync(job, cancellationToken);

                        yield return new StreamEvent { Type = StreamEventType.Error, Error = job.Error, Status = "Failed", PartialContent = job.Result };
                        yield break;
                    }
                }

                var timeSinceLastData = DateTime.UtcNow - lastDataTime;
                if (isProcessing && timeSinceLastData > TimeSpan.FromSeconds(60))
                {
                    job.Status = "Failed";
                    job.Error = "Job timed out - no response from AI service";
                    await _jobRepository.UpdateAsync(job, cancellationToken);

                    yield return new StreamEvent { Type = StreamEventType.Error, Error = job.Error, Status = "Failed", PartialContent = job.Result };
                    yield break;
                }

                if (job.Status == "Completed")
                {
                    if (job.Result?.Length > lastLength)
                    {
                        yield return new StreamEvent { Type = StreamEventType.Chunk, Content = job.Result.Substring(lastLength), Status = "Completed", Progress = 100 };
                    }
                    yield return new StreamEvent { Type = StreamEventType.Done, IsRaw = true, RawValue = "[DONE]" };
                    yield break;
                }

                if (job.Status == "Failed" || job.Status == "Cancelled")
                {
                    yield return new StreamEvent { Type = StreamEventType.Error, Error = job.Error ?? "Error", Status = job.Status, PartialContent = job.Result };
                    yield break;
                }

                if (isProcessing && noDataCount % 10 == 0)
                {
                    var timeElapsed = DateTime.UtcNow - startTime;
                    yield return new StreamEvent { Type = StreamEventType.Heartbeat, Status = job.Status, Progress = job.Progress, WaitingTime = timeElapsed.ToString(@"mm\:ss") };
                }

                await Task.Delay(500, cancellationToken);
            }
        }
    }
}
