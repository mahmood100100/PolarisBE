using MediatR;

namespace Polaris.Application.Features.Conversations.Commands.StreamChat
{
    public class StreamChatCommand : IRequest<StreamChatResult>
    {
        /// <summary>The ID of the conversation to append this message to.</summary>
        public Guid ConversationId { get; set; }

        /// <summary>The user's new message content.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>The authenticated user's ID.</summary>
        public Guid UserId { get; set; }
    }
}
