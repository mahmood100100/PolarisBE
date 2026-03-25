using MediatR;
using System.Text.Json.Serialization;

namespace Polaris.Application.Features.Conversations.Commands.DeleteConversation
{
    public class DeleteConversationCommand : IRequest<bool>
    {
        public Guid ConversationId { get; set; }

        [JsonIgnore]
        public Guid UserId { get; set; }
    }
}
