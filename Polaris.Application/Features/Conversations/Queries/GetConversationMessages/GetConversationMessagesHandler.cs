using MediatR;
using Polaris.Domain.Entities;
using Polaris.Domain.Interfaces.IRepositories;

namespace Polaris.Application.Features.Conversations.Queries.GetConversationMessages
{
    public class GetConversationMessagesHandler : IRequestHandler<GetConversationMessagesQuery, List<Message>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetConversationMessagesHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<Message>> Handle(GetConversationMessagesQuery request, CancellationToken cancellationToken)
        {
            // Verify access first
            var hasAccess = await _unitOfWork.Conversations.ExistsForUserAsync(request.ConversationId, request.UserId, cancellationToken);
            if (!hasAccess)
            {
                 // You might prefer returning empty or throwing an exception. Returning empty is safer.
                return new List<Message>();
            }

            return await _unitOfWork.Messages.GetByConversationIdAsync(request.ConversationId, cancellationToken);
        }
    }
}
