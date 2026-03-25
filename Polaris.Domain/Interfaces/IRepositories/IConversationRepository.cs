using Polaris.Domain.Entities;

namespace Polaris.Domain.Interfaces.IRepositories
{
    /// <summary>
    /// Repository interface for Conversation entities.
    /// Defines the data access contract for chat conversations,
    /// following the Repository pattern of Clean Architecture.
    /// </summary>
    public interface IConversationRepository
    {
        /// <summary>Creates a new conversation and persists it to the database.</summary>
        Task<Conversation> CreateAsync(Conversation conversation, CancellationToken cancellationToken = default);

        /// <summary>Retrieves a conversation by its unique ID (without messages).</summary>
        Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a conversation by its unique ID including all its messages,
        /// ordered chronologically (oldest first).
        /// </summary>
        Task<Conversation?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all conversations for a specific user, ordered by creation date (newest first).
        /// Does NOT include messages to keep the payload lightweight.
        /// </summary>
        Task<List<Conversation>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all conversations linked to a specific project for a user,
        /// ordered by creation date (newest first).
        /// </summary>
        Task<List<Conversation>> GetByProjectIdAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Updates the title or model of an existing conversation.</summary>
        Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default);

        /// <summary>Deletes a conversation and all its messages (cascade).</summary>
        Task DeleteAsync(Conversation conversation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks that the conversation exists and belongs to the given user.
        /// Used for access control validation.
        /// </summary>
        Task<bool> ExistsForUserAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
    }
}
