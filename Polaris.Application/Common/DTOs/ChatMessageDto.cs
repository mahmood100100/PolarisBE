namespace Polaris.Application.Common.DTOs
{
    /// <summary>
    /// Represents a single message in a chat conversation history.
    /// Used to pass the ordered turn list to the AI service so the model
    /// has full context of the prior conversation.
    ///
    /// Maps to the DeepSeek/OpenAI chat completion "messages" array.
    /// </summary>
    public class ChatMessageDto
    {
        /// <summary>
        /// The role of the message author.
        /// Accepted values: "system", "user", "assistant".
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>The text content of the message.</summary>
        public string Content { get; set; } = string.Empty;
    }
}
