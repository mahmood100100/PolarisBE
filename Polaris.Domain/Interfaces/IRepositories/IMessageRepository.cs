using Polaris.Domain.Entities;

namespace Polaris.Domain.Interfaces.IRepositories
{
    /// <summary>
    /// Repository interface for Message entities.
    /// Defines the data access contract for chat messages,
    /// following the Repository pattern of Clean Architecture.
    /// </summary>
    public interface IMessageRepository
    {
        /// <summary>Persists a new message to the database.</summary>
        Task<Message> CreateAsync(Message message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all messages for a given conversation, ordered chronologically (oldest first).
        /// </summary>
        Task<List<Message>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);

        /// <summary>Updates an existing message (e.g., appending streamed AI content).</summary>
        Task UpdateAsync(Message message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the last N messages for a conversation to build the context
        /// window for the AI (history-aware chat).
        /// </summary>
        Task<List<Message>> GetLastNMessagesAsync(Guid conversationId, int count, CancellationToken cancellationToken = default);
    }
}
