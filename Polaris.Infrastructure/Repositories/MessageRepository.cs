using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polaris.Domain.Entities;
using Polaris.Domain.Interfaces.IRepositories;
using Polaris.Infrastructure.Data;

namespace Polaris.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for Message entities.
    /// Provides queries for chat messages using Entity Framework Core.
    /// Note: SaveChanges is NOT called here — that responsibility belongs to UnitOfWork.
    /// </summary>
    public class MessageRepository : IMessageRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MessageRepository> _logger;

        public MessageRepository(AppDbContext context, ILogger<MessageRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<Message> CreateAsync(
            Message message,
            CancellationToken cancellationToken = default)
        {
            await _context.Messages.AddAsync(message, cancellationToken);
            _logger.LogInformation(
                "Message {MessageId} queued for conversation {ConversationId}",
                message.Id, message.ConversationId);
            return message;
        }

        /// <inheritdoc/>
        public async Task<List<Message>> GetByConversationIdAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SentAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(
            Message message,
            CancellationToken cancellationToken = default)
        {
            _context.Messages.Update(message);
            _logger.LogInformation("Message {MessageId} updated", message.Id);
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<List<Message>> GetLastNMessagesAsync(
            Guid conversationId,
            int count,
            CancellationToken cancellationToken = default)
        {
            // Fetch the last N messages and re-order ascending for AI context window
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentAt)
                .Take(count)
                .OrderBy(m => m.SentAt)           // restore chronological order for the AI
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
    }
}
