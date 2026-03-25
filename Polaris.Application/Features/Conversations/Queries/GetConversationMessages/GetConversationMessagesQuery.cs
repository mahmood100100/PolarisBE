using MediatR;
using Polaris.Domain.Entities;

namespace Polaris.Application.Features.Conversations.Queries.GetConversationMessages
{
    public class GetConversationMessagesQuery : IRequest<List<Message>>
    {
        public Guid ConversationId { get; set; }
        public Guid UserId { get; set; }
    }
}
