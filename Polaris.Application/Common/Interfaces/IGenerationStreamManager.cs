using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;

namespace Polaris.Application.Common.Interfaces
{
    /// <summary>
    /// Manages in-memory streaming channels for generation jobs.
    /// Allows the background processor to push chunks in real-time,
    /// and the stream endpoint to read them word-by-word.
    /// </summary>
    public interface IGenerationStreamManager
    {
        /// <summary>
        /// Creates a new channel for a job. Call this when the job starts processing.
        /// </summary>
        void CreateChannel(Guid jobId);

        /// <summary>
        /// Writes a chunk to the job's channel. Called by the background processor.
        /// </summary>
        ValueTask WriteAsync(Guid jobId, string chunk, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks the channel as complete (no more data). Called when the job finishes.
        /// </summary>
        void Complete(Guid jobId, Exception? error = null);

        /// <summary>
        /// Reads chunks from the job's channel as an async enumerable.
        /// </summary>
        IAsyncEnumerable<string> ReadAsync(Guid jobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the number of chunks written so far (for calculating what was already sent).
        /// </summary>
        int GetWrittenChunkCount(Guid jobId);

        /// <summary>
        /// Checks if a channel exists for the given job.
        /// </summary>
        bool HasChannel(Guid jobId);

        /// <summary>
        /// Removes and cleans up the channel for a job.
        /// </summary>
        void RemoveChannel(Guid jobId);
    }
}
