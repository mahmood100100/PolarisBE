using Polaris.Application.Common.DTOs;

namespace Polaris.Application.Common.Interfaces
{
    /// <summary>
    /// Service interface for communicating with the DeepSeek AI API.
    /// Supports both streaming (real-time token delivery) and non-streaming
    /// (complete response) generation modes.
    ///
    /// Both methods now accept a list of <see cref="ChatMessageDto"/> to enable
    /// history-aware conversations where the AI has full context of prior exchanges.
    /// </summary>
    public interface IDeepSeekAIService
    {
        // ─── Chat / Conversation API ─────────────────────────────────────────

        /// <summary>
        /// Streams a chat response from the AI given a full conversation history.
        /// Each item in <paramref name="messages"/> represents a prior turn in the
        /// conversation (User or Assistant), giving the model full context.
        /// Returns an async enumerable of content tokens yielded in real-time.
        /// </summary>
        /// <param name="messages">The full ordered chat history (oldest first).</param>
        /// <param name="cancellationToken">Token to cancel the streaming operation.</param>
        IAsyncEnumerable<string> StreamChatAsync(
            IEnumerable<ChatMessageDto> messages,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a chat response in non-streaming mode given a conversation history.
        /// Waits for the complete response before returning.
        /// Use for short prompts or when streaming is not required.
        /// </summary>
        /// <param name="messages">The full ordered chat history (oldest first).</param>
        /// <param name="cancellationToken">Token to cancel the request.</param>
        Task<string> GenerateChatAsync(
            IEnumerable<ChatMessageDto> messages,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Advanced Agentic Feature: Analyzes the conversation history to determine if the AI 
        /// wants to invoke any tools (e.g. scrape_website or search_web). Returns the tool name and its arguments JSON string if needed.
        /// </summary>
        Task<(string ToolName, string Arguments)?> AnalyzeForToolsAsync(IEnumerable<ChatMessageDto> messages, CancellationToken cancellationToken = default);

        // ─── Legacy / Code Generation API ───────────────────────────────────

        /// <summary>
        /// Streams code generation results as they are produced by the AI model.
        /// Returns an async enumerable of content tokens (words/code fragments).
        /// </summary>
        /// <param name="prompt">The user's prompt describing what code to generate.</param>
        /// <param name="cancellationToken">Token to cancel the streaming operation.</param>
        IAsyncEnumerable<string> StreamCodeAsync(
            string prompt,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates code in non-streaming mode. Waits for the complete response.
        /// </summary>
        /// <param name="prompt">The user's prompt describing what code to generate.</param>
        /// <param name="cancellationToken">Token to cancel the request.</param>
        Task<string> GenerateCodeAsync(
            string prompt,
            CancellationToken cancellationToken = default);
    }
}
