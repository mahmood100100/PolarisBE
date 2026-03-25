using MediatR;
using Polaris.Domain.Interfaces.IRepositories;
using System.Threading;
using System.Threading.Tasks;

namespace Polaris.Application.Features.Conversations.Commands.DeleteConversation
{
    public class DeleteConversationHandler : IRequestHandler<DeleteConversationCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteConversationHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteConversationCommand request, CancellationToken cancellationToken)
        {
            // 1. Get the conversation
            var conversation = await _unitOfWork.Conversations.GetByIdAsync(request.ConversationId, cancellationToken);
            
            // 2. Validate existence and ownership
            if (conversation == null || conversation.UserId != request.UserId)
            {
                return false;
            }

            // 3. Delete the conversation (Cascade should handle messages if configured, 
            // otherwise relying on EF Core's Delete behavior)
            await _unitOfWork.Conversations.DeleteAsync(conversation, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return true;
        }
    }
}
