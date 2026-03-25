using MediatR;
using Polaris.Domain.Entities;
using Polaris.Domain.Interfaces.IRepositories;

namespace Polaris.Application.Features.Conversations.Queries.GetConversations
{
    public class GetConversationsHandler : IRequestHandler<GetConversationsQuery, List<Conversation>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetConversationsHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<Conversation>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
        {
            if (request.ProjectId.HasValue)
            {
                return await _unitOfWork.Conversations.GetByProjectIdAsync(request.ProjectId.Value, request.UserId, cancellationToken);
            }
            
            return await _unitOfWork.Conversations.GetByUserIdAsync(request.UserId, cancellationToken);
        }
    }
}
