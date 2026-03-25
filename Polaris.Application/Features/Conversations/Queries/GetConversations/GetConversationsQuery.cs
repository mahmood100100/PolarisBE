using MediatR;
using Polaris.Domain.Entities;

namespace Polaris.Application.Features.Conversations.Queries.GetConversations
{
    public class GetConversationsQuery : IRequest<List<Conversation>>
    {
        public Guid UserId { get; set; }
        
        /// <summary>
        /// Optional: If provided, will only return conversations belonging to this project.
        /// </summary>
        public Guid? ProjectId { get; set; }
    }
}
