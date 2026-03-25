namespace Polaris.Application.Features.Conversations.Commands.CreateConversation
{
    public class CreateConversationResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Guid? ProjectId { get; set; }
    }
}
