namespace Polaris.Application.Common.Interfaces
{
    /// <summary>
    /// Interface for the background job processor that handles AI chat generation.
    /// Implementations are invoked by Hangfire to process chat messages asynchronously,
    /// so the user can refresh the page and reconnect to the ongoing SSE stream.
    /// </summary>
    public interface IChatJobProcessor
    {
        Task ProcessChatAsync(Guid jobId, Guid conversationId, string message, Guid userId);
    }
}
