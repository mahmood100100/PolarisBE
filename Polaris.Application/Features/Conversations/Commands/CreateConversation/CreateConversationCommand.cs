using MediatR;

namespace Polaris.Application.Features.Conversations.Commands.CreateConversation
{
    /// <summary>
    /// Command to create a new chat conversation.
    /// A conversation can optionally be linked to a project.
    /// The first message title can be overridden later via UpdateConversation.
    /// </summary>
    public class CreateConversationCommand : IRequest<CreateConversationResponse>
    {
        /// <summary>
        /// Optional title for the conversation.
        /// If not provided, defaults to "New Chat".
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The AI model to use for this conversation (e.g., "deepseek-chat").
        /// Defaults to "deepseek-chat" if not specified.
        /// </summary>
        public string? ModelName { get; set; }

        /// <summary>
        /// Optional project ID to associate this conversation with a project context.
        /// Pass null for a standalone conversation.
        /// </summary>
        public Guid? ProjectId { get; set; }

        /// <summary>The authenticated user's ID (set by the controller from JWT claims).</summary>
        public Guid UserId { get; set; }
    }
}
