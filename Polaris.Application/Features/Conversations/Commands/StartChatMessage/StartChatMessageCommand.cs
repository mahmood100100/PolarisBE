using MediatR;

namespace Polaris.Application.Features.Conversations.Commands.StartChatMessage
{
    public class StartChatMessageCommand : IRequest<StartChatMessageResponse>
    {
        public Guid ConversationId { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid UserId { get; set; }
    }
}
