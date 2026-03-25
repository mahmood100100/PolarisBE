namespace Polaris.Application.Features.Conversations.Commands.StreamChat
{
    public class StreamChatResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }

        /// <summary>
        /// An asynchronous stream of response chunks from the AI.
        /// </summary>
        public IAsyncEnumerable<string>? ContentStream { get; set; }
    }
}
