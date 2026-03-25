using MediatR;
using Polaris.Domain.Entities;
using Polaris.Domain.Interfaces.IRepositories;

namespace Polaris.Application.Features.Conversations.Commands.CreateConversation
{
    public class CreateConversationHandler : IRequestHandler<CreateConversationCommand, CreateConversationResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateConversationHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CreateConversationResponse> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
        {
            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Title = string.IsNullOrWhiteSpace(request.Title) ? "New Chat" : request.Title,
                ModelName = string.IsNullOrWhiteSpace(request.ModelName) ? "deepseek-chat" : request.ModelName,
                ProjectId = request.ProjectId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Conversations.CreateAsync(conversation, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return new CreateConversationResponse
            {
                Id = conversation.Id,
                Title = conversation.Title,
                ModelName = conversation.ModelName,
                CreatedAt = conversation.CreatedAt,
                ProjectId = conversation.ProjectId
            };
        }
    }
}
