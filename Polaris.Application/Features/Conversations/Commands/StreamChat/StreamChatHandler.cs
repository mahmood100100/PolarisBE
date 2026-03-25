using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polaris.Application.Common.DTOs;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Entities;
using Polaris.Domain.Enums;
using Polaris.Domain.Interfaces.IRepositories;
using System.Runtime.CompilerServices;
using System.Text;

namespace Polaris.Application.Features.Conversations.Commands.StreamChat
{
    public class StreamChatHandler : IRequestHandler<StreamChatCommand, StreamChatResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDeepSeekAIService _aiService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StreamChatHandler> _logger;

        public StreamChatHandler(
            IUnitOfWork unitOfWork,
            IDeepSeekAIService aiService,
            IServiceProvider serviceProvider,
            ILogger<StreamChatHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _aiService = aiService;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<StreamChatResult> Handle(StreamChatCommand request, CancellationToken cancellationToken)
        {
            // 1. Validate if conversation exists & belongs to the user
            var hasAccess = await _unitOfWork.Conversations.ExistsForUserAsync(request.ConversationId, request.UserId, cancellationToken);
            if (!hasAccess)
            {
                _logger.LogWarning("Unauthorized access or conversation not found: {ConversationId}", request.ConversationId);
                return new StreamChatResult { Success = false, Error = "Conversation not found or unauthorized access." };
            }

            // 2. Save the User's Message immediately
            var userMessage = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = request.ConversationId,
                Content = request.Message,
                Role = MessageRole.User,
                SentAt = DateTime.UtcNow,
                Status = MessageStatus.Completed
            };

            await _unitOfWork.Messages.CreateAsync(userMessage, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            // 3. Optional: Set Title if this is the first message 
            // (You can omit this if you prefer letting the user set it manually later)
            var allMessages = await _unitOfWork.Messages.GetByConversationIdAsync(request.ConversationId, cancellationToken);
            if (allMessages.Count == 1)
            {
                var conv = await _unitOfWork.Conversations.GetByIdAsync(request.ConversationId, cancellationToken);
                if (conv != null && conv.Title == "New Chat")
                {
                    // Generate a quick title from the first prompt
                    conv.Title = request.Message.Length > 40 ? request.Message.Substring(0, 40) + "..." : request.Message;
                    await _unitOfWork.Conversations.UpdateAsync(conv, cancellationToken);
                    await _unitOfWork.CompleteAsync(cancellationToken);
                }
            }

            // 4. Load Conversation History (last 20 messages for context)
            var history = await _unitOfWork.Messages.GetLastNMessagesAsync(request.ConversationId, 20, cancellationToken);
            
            var dtoList = history.Select(m => new ChatMessageDto
            {
                Role = m.Role.ToString().ToLower(), // Enum to string (user, assistant, system)
                Content = m.Content
            }).ToList();

            // 5. Stream from AI
            var aiStream = _aiService.StreamChatAsync(dtoList, cancellationToken);

            // 6. Wrap the stream so we can Capture & Save the final result after it's fully streamed
            var savingStream = ProcessAndSaveAssistantMessageAsync(request.ConversationId, aiStream);

            return new StreamChatResult
            {
                Success = true,
                ContentStream = savingStream
            };
        }

        /// <summary>
        /// This method acts as a wrapper around the AI stream to intercept the chunks.
        /// Once the streaming naturally finishes (or is cancelled), it takes the accumulated
        /// text and saves the Assistant's message to the Database.
        /// </summary>
        private async IAsyncEnumerable<string> ProcessAndSaveAssistantMessageAsync(
            Guid conversationId, 
            IAsyncEnumerable<string> aiStream,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var sb = new StringBuilder();

            await foreach (var chunk in aiStream.WithCancellation(cancellationToken))
            {
                sb.Append(chunk);
                // Yield immediately so the SSE API can push it to the frontend
                yield return chunk;
            }

            // 7. Streaming Done -> Save to DB
            // We use a separate DI scope because by the time this executes, the original HTTP Request scope
            // might be entering a "Disposed" phase, and concurrent DbContext usage is tricky.
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedUoW = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var assistantMessage = new Message
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversationId,
                    Content = sb.ToString(),
                    Role = MessageRole.Assistant,
                    SentAt = DateTime.UtcNow,
                    Status = MessageStatus.Completed
                };

                await scopedUoW.Messages.CreateAsync(assistantMessage);
                await scopedUoW.CompleteAsync();
                
                _logger.LogInformation("Successfully saved AI response to conversation {ConversationId}", conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save AI response to DB for conversation {ConversationId}", conversationId);
            }
        }
    }
}
