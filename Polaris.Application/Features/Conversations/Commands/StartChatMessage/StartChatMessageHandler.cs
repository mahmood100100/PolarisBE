using MediatR;
using Microsoft.Extensions.Logging;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Entities;
using Polaris.Domain.Enums;
using Polaris.Domain.Interfaces.IRepositories;

namespace Polaris.Application.Features.Conversations.Commands.StartChatMessage
{
    public class StartChatMessageHandler : IRequestHandler<StartChatMessageCommand, StartChatMessageResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenerationJobRepository _jobRepository;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly ILogger<StartChatMessageHandler> _logger;

        public StartChatMessageHandler(
            IUnitOfWork unitOfWork,
            IGenerationJobRepository jobRepository,
            IBackgroundJobService backgroundJobService,
            ILogger<StartChatMessageHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _jobRepository = jobRepository;
            _backgroundJobService = backgroundJobService;
            _logger = logger;
        }

        public async Task<StartChatMessageResponse> Handle(StartChatMessageCommand request, CancellationToken cancellationToken)
        {
            // 1. Verify access
            var hasAccess = await _unitOfWork.Conversations.ExistsForUserAsync(request.ConversationId, request.UserId, cancellationToken);
            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("Conversation not found or access denied.");
            }

            // 2. Save User Message immediately
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

            // 3. Update Conversation Title if it's the very first message
            var allMessages = await _unitOfWork.Messages.GetByConversationIdAsync(request.ConversationId, cancellationToken);
            if (allMessages.Count == 1)
            {
                var conv = await _unitOfWork.Conversations.GetByIdAsync(request.ConversationId, cancellationToken);
                if (conv != null && conv.Title == "New Chat")
                {
                    conv.Title = request.Message.Length > 40 ? request.Message.Substring(0, 40) + "..." : request.Message;
                    await _unitOfWork.Conversations.UpdateAsync(conv, cancellationToken);
                    await _unitOfWork.CompleteAsync(cancellationToken);
                }
            }

            // 4. Create GenerationJob for tracking SSE Stream
            var job = new GenerationJob
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                ConversationId = request.ConversationId, // Tracks which chat owns this background job!
                Prompt = "Chat Message Sync", 
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UserIntent = "active",
                Progress = 0
            };

            await _jobRepository.CreateAsync(job, cancellationToken);

            // 5. Enqueue Handfire Processor
            var hangfireJobId = _backgroundJobService.Enqueue<IChatJobProcessor>(
                processor => processor.ProcessChatAsync(job.Id, request.ConversationId, request.Message, request.UserId));

            job.HangfireJobId = hangfireJobId;
            await _jobRepository.UpdateAsync(job, cancellationToken);

            _logger.LogInformation("Background chat job {JobId} enqueued for conversation {ConversationId}", job.Id, request.ConversationId);

            return new StartChatMessageResponse
            {
                JobId = job.Id,
                ConversationId = request.ConversationId,
                Status = "processing",
                Message = "Chat stream has been initiated in the background"
            };
        }
    }
}
