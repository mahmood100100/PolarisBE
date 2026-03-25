using Microsoft.Extensions.Logging;
using Polaris.Application.Common.Interfaces;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Polaris.Infrastructure.Services
{
    /// <summary>
    /// Singleton service that manages in-memory streaming channels for generation jobs.
    /// 
    /// Each generation job gets its own Channel&lt;string&gt; for real-time chunk delivery.
    /// The background job processor writes chunks to the channel, and the SSE streaming
    /// endpoint reads them. This enables word-by-word real-time delivery to clients
    /// without relying on database polling.
    /// 
    /// Why Singleton?
    ///   The Hangfire background worker and the HTTP streaming endpoint run in different
    ///   DI scopes. A Singleton lifetime ensures both sides reference the same channel
    ///   instance, enabling cross-scope real-time communication.
    /// 
    /// Channel lifecycle:
    ///   1. CreateChannel() — called when the background processor starts a job
    ///   2. WriteAsync() — called for each chunk received from the AI service
    ///   3. Complete() — called when the job finishes (success or failure)
    ///   4. ReadAsync() — called by the SSE endpoint to consume chunks
    ///   5. RemoveChannel() — called after the SSE endpoint finishes reading
    /// </summary>
    public class GenerationStreamManager : IGenerationStreamManager
    {
        private readonly ConcurrentDictionary<Guid, ChannelState> _channels = new();
        private readonly ILogger<GenerationStreamManager> _logger;

        public GenerationStreamManager(ILogger<GenerationStreamManager> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates a new unbounded channel for real-time chunk streaming.
        /// SingleWriter is true because only the background processor writes.
        /// SingleReader is false to allow client reconnections (multiple readers).
        /// </summary>
        public void CreateChannel(Guid jobId)
        {
            var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleWriter = true,
                SingleReader = false
            });

            var state = new ChannelState(channel);

            if (_channels.TryAdd(jobId, state))
            {
                _logger.LogInformation("Channel created for job {JobId}", jobId);
            }
            else
            {
                // Replace existing channel if one already exists (edge case: job retry)
                _logger.LogWarning("Channel already exists for job {JobId}, replacing", jobId);
                _channels[jobId] = state;
            }
        }

        /// <summary>
        /// Writes a chunk (word/token) to the job's channel.
        /// Called by the background processor for each chunk received from the AI service.
        /// </summary>
        public async ValueTask WriteAsync(
            Guid jobId,
            string chunk,
            CancellationToken cancellationToken = default)
        {
            if (_channels.TryGetValue(jobId, out var state))
            {
                await state.Channel.Writer.WriteAsync(chunk, cancellationToken);
                Interlocked.Increment(ref state.WrittenCount);
            }
            else
            {
                _logger.LogWarning(
                    "Attempted to write to non-existent channel for job {JobId}", jobId);
            }
        }

        /// <summary>
        /// Marks the channel as complete, signaling that no more data will be written.
        /// Called by the background processor when the job finishes (success, failure, or cancellation).
        /// If an error is provided, it's propagated to the reader as an exception.
        /// </summary>
        public void Complete(Guid jobId, Exception? error = null)
        {
            if (_channels.TryGetValue(jobId, out var state))
            {
                if (error != null)
                {
                    state.Channel.Writer.TryComplete(error);
                    _logger.LogWarning(
                        "Channel completed with error for job {JobId}: {Error}",
                        jobId, error.Message);
                }
                else
                {
                    state.Channel.Writer.TryComplete();
                    _logger.LogInformation("Channel completed for job {JobId}", jobId);
                }
            }
        }

        /// <summary>
        /// Reads all chunks from the job's channel as an async enumerable.
        /// This method blocks (asynchronously) until data is available, and completes
        /// when the writer calls Complete().
        /// 
        /// Used by the SSE streaming endpoint to deliver chunks to the client in real-time.
        /// </summary>
        public async IAsyncEnumerable<string> ReadAsync(
            Guid jobId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!_channels.TryGetValue(jobId, out var state))
            {
                _logger.LogWarning("No channel found for job {JobId}", jobId);
                yield break;
            }

            var reader = state.Channel.Reader;

            await foreach (var chunk in reader.ReadAllAsync(cancellationToken))
            {
                yield return chunk;
            }
        }

        /// <summary>
        /// Returns the total number of chunks written to the job's channel so far.
        /// Used for tracking progress and calculating catch-up offsets.
        /// </summary>
        public int GetWrittenChunkCount(Guid jobId)
        {
            return _channels.TryGetValue(jobId, out var state) ? state.WrittenCount : 0;
        }

        /// <summary>
        /// Checks if a channel exists for the given job.
        /// Returns false if the server was restarted (channels are in-memory only).
        /// </summary>
        public bool HasChannel(Guid jobId)
        {
            return _channels.ContainsKey(jobId);
        }

        /// <summary>
        /// Removes and cleans up the channel for a completed job.
        /// Ensures the writer is completed before removal to prevent data loss.
        /// </summary>
        public void RemoveChannel(Guid jobId)
        {
            if (_channels.TryRemove(jobId, out var state))
            {
                state.Channel.Writer.TryComplete();
                _logger.LogInformation("Channel removed for job {JobId}", jobId);
            }
        }

        /// <summary>
        /// Internal state wrapper for each channel, holding the channel reference
        /// and a count of how many chunks have been written (for progress tracking).
        /// </summary>
        private class ChannelState
        {
            public Channel<string> Channel { get; }
            public int WrittenCount;

            public ChannelState(Channel<string> channel)
            {
                Channel = channel;
                WrittenCount = 0;
            }
        }
    }
}
