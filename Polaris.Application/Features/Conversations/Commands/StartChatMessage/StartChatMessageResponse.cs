namespace Polaris.Application.Features.Conversations.Commands.StartChatMessage
{
    public class StartChatMessageResponse
    {
        public Guid JobId { get; set; }
        public Guid ConversationId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
