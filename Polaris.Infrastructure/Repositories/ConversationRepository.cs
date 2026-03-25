using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polaris.Domain.Entities;
using Polaris.Domain.Interfaces.IRepositories;
using Polaris.Infrastructure.Data;

namespace Polaris.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for Conversation entities.
    /// Provides CRUD operations and user-scoped queries for chat conversations
    /// using Entity Framework Core and PostgreSQL.
    /// </summary>
    public class ConversationRepository : IConversationRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ConversationRepository> _logger;

        public ConversationRepository(AppDbContext context, ILogger<ConversationRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<Conversation> CreateAsync(
            Conversation conversation,
            CancellationToken cancellationToken = default)
        {
            await _context.Conversations.AddAsync(conversation, cancellationToken);
            _logger.LogInformation(
                "Conversation {ConversationId} created for user {UserId}",
                conversation.Id, conversation.UserId);
            return conversation;
        }

        /// <inheritdoc/>
        public async Task<Conversation?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Conversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Conversation?> GetByIdWithMessagesAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Conversations
                .Include(c => c.Messages.OrderBy(m => m.SentAt))
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<Conversation>> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Conversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<Conversation>> GetByProjectIdAsync(
            Guid projectId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Conversations
                .Where(c => c.ProjectId == projectId && c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(
            Conversation conversation,
            CancellationToken cancellationToken = default)
        {
            _context.Conversations.Update(conversation);
            _logger.LogInformation("Conversation {ConversationId} updated", conversation.Id);
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(
            Conversation conversation,
            CancellationToken cancellationToken = default)
        {
            _context.Conversations.Remove(conversation);
            _logger.LogInformation("Conversation {ConversationId} deleted", conversation.Id);
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsForUserAsync(
            Guid conversationId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Conversations
                .AnyAsync(c => c.Id == conversationId && c.UserId == userId, cancellationToken);
        }
    }
}
