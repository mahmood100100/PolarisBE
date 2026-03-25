using Microsoft.Extensions.Logging;
using Polaris.Application.Common.DTOs;
using Polaris.Application.Common.Interfaces;
using Polaris.Infrastructure.DTOs.DeepSeek;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Polaris.Infrastructure.Services
{
    /// <summary>
    /// Service implementation for communicating with the DeepSeek AI API.
    /// Provides both streaming and non-streaming modes for:
    ///   - Chat (history-aware conversations via <see cref="StreamChatAsync"/> / <see cref="GenerateChatAsync"/>)
    ///   - Code generation (single-prompt via <see cref="StreamCodeAsync"/> / <see cref="GenerateCodeAsync"/>)
    ///
    /// Streaming mode:
    ///   - Uses HTTP chunked transfer encoding to receive tokens in real-time
    ///   - Returns an IAsyncEnumerable that yields individual content tokens
    ///   - Parses the DeepSeek SSE (Server-Sent Events) response format
    ///
    /// Configuration:
    ///   - API key and base URL are injected via HttpClient DI registration
    /// </summary>
    public class DeepSeekService : IDeepSeekAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DeepSeekService> _logger;

        // Default system prompt for the coding assistant persona
        private const string CodingSystemPrompt =
            "You are a professional coding assistant. Write clean, efficient, and well-documented code.";

        // Default model identifier
        private const string ModelName = "deepseek-chat";

        public DeepSeekService(HttpClient httpClient, ILogger<DeepSeekService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════════════════
        // CHAT API — History-aware conversation methods
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Streams a chat response from the DeepSeek API given the full conversation history.
        /// Each item in <paramref name="messages"/> is sent as a turn in the "messages" array,
        /// giving the model full context of prior exchanges.
        ///
        /// SSE stream format:
        ///   data: {"choices":[{"delta":{"content":"token"}}]}
        ///   data: [DONE]
        /// </summary>
        public async IAsyncEnumerable<string> StreamChatAsync(
            IEnumerable<ChatMessageDto> messages,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var payload = BuildChatPayload(messages, stream: true);

            _logger.LogInformation("Starting DeepSeek chat stream request");

            var httpRequest = BuildHttpRequest(payload);
            HttpResponseMessage response;

            try
            {
                response = await _httpClient.SendAsync(
                    httpRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during DeepSeek chat streaming");
                throw;
            }

            using (response)
            {
                HandleErrorStatusCodes(response);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                string? line;
                while ((line = await reader.ReadLineAsync()) != null
                       && !cancellationToken.IsCancellationRequested)
                {
                    var content = ParseSseLine(line);
                    if (content == null) continue;
                    if (content == "[DONE]")
                    {
                        _logger.LogInformation("DeepSeek chat stream completed");
                        yield break;
                    }
                    yield return content;
                }
            }
        }

        /// <summary>
        /// Generates a chat response in non-streaming mode given the full conversation history.
        /// </summary>
        public async Task<string> GenerateChatAsync(
            IEnumerable<ChatMessageDto> messages,
            CancellationToken cancellationToken = default)
        {
            var payload = BuildChatPayload(messages, stream: false);

            _logger.LogInformation("Starting DeepSeek chat non-streaming request");

            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "v1/chat/completions", payload, cancellationToken);

                HandleErrorStatusCodes(response);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<DeepSeekResponse>(json);
                var content = result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;

                _logger.LogInformation(
                    "DeepSeek chat non-streaming completed. Length: {Length}", content.Length);
                return content;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during DeepSeek chat generation");
                throw;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // AGENT / TOOL CALLING API 
        // ═══════════════════════════════════════════════════════════════════

        public async Task<(string ToolName, string Arguments)?> AnalyzeForToolsAsync(
            IEnumerable<ChatMessageDto> messages,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting DeepSeek Tool analysis (Function Calling)");

            // We construct a specific payload just for tool detection
            var payload = new
            {
                model = ModelName,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }),
                tools = new object[]
                {
                    new
                    {
                        type = "function",
                        function = new
                        {
                            name = "scrape_website",
                            description = "Fetches the raw textual content of a specific URL. Use this tool ONLY when you need to read an article, read documentation, or read a website link the user mentions to answer their question correctly.",
                            parameters = new
                            {
                                type = "object",
                                properties = new
                                {
                                    url = new { type = "string", description = "The full HTTPS URL that you want to read." }
                                },
                                required = new[] { "url" }
                            }
                        }
                    },
                    new
                    {
                        type = "function",
                        function = new
                        {
                            name = "search_web",
                            description = "Searches the internet for recent or real-time information. Use this tool when you don't know the answer, when the user asks for news, or when the user asks you to search for something.",
                            parameters = new
                            {
                                type = "object",
                                properties = new
                                {
                                    query = new { type = "string", description = "The search query to look up on the internet." }
                                },
                                required = new[] { "query" }
                            }
                        }
                    }
                },
                stream = false,
                temperature = 0.1 // Low temp for more deterministic tool calling
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("v1/chat/completions", payload, cancellationToken);
                HandleErrorStatusCodes(response);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                
                // We parse it manually to handle the tool_calls array without modifying our simple DTOs
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message))
                    {
                        if (message.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.GetArrayLength() > 0)
                        {
                            var toolCall = toolCalls[0];
                            if (toolCall.TryGetProperty("function", out var function))
                            {
                                var functionName = function.GetProperty("name").GetString();
                                if (function.TryGetProperty("arguments", out var argumentsStr))
                                {
                                    _logger.LogInformation("DeepSeek requested Tool Call: {ToolName} with Args: {Args}", functionName, argumentsStr.GetString());
                                    return (functionName, argumentsStr.GetString())!;
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation("DeepSeek did not request any tools.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during DeepSeek Tool analysis");
                return null; // Gracefully fail and just answer normally without tools
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // CODE GENERATION API — Single-prompt methods (legacy)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Streams code generation results from the DeepSeek API as an async enumerable.
        /// Sends a single user prompt with a predefined coding system prompt.
        ///
        /// How it works:
        /// 1. Wraps the prompt in a two-message array: [system, user]
        /// 2. Delegates to <see cref="StreamChatAsync"/> for actual streaming
        /// </summary>
        public IAsyncEnumerable<string> StreamCodeAsync(
            string prompt,
            CancellationToken cancellationToken = default)
        {
            var messages = BuildSinglePromptMessages(prompt);
            return StreamChatAsync(messages, cancellationToken);
        }

        /// <summary>
        /// Generates code using the DeepSeek API in non-streaming mode.
        /// Wraps the single prompt and delegates to <see cref="GenerateChatAsync"/>.
        /// </summary>
        public Task<string> GenerateCodeAsync(
            string prompt,
            CancellationToken cancellationToken = default)
        {
            var messages = BuildSinglePromptMessages(prompt);
            return GenerateChatAsync(messages, cancellationToken);
        }

        // ═══════════════════════════════════════════════════════════════════
        // Private Helpers
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Builds the DeepSeek chat completions request payload from a list of messages.
        /// </summary>
        private static object BuildChatPayload(IEnumerable<ChatMessageDto> messages, bool stream)
        {
            return new
            {
                model = ModelName,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }),
                stream,
                temperature = 0.7,
                max_tokens = 4000
            };
        }

        /// <summary>
        /// Wraps a single user prompt in the standard [system, user] message format
        /// used by the code generation endpoints.
        /// </summary>
        private static IEnumerable<ChatMessageDto> BuildSinglePromptMessages(string prompt)
        {
            return new[]
            {
                new ChatMessageDto { Role = "system", Content = CodingSystemPrompt },
                new ChatMessageDto { Role = "user",   Content = prompt }
            };
        }

        /// <summary>
        /// Creates an HttpRequestMessage targeting the DeepSeek chat completions endpoint.
        /// Uses ResponseHeadersRead for streaming efficiency.
        /// </summary>
        private static HttpRequestMessage BuildHttpRequest(object payload)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions");
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");
            return request;
        }

        /// <summary>
        /// Parses a single SSE data line and returns:
        ///   - null      → skip this line (empty or non-data)
        ///   - "[DONE]"  → stream is complete
        ///   - string    → the content token extracted from the JSON chunk
        /// </summary>
        private string? ParseSseLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                return null;

            var data = line[6..]; // strip "data: " prefix

            if (data == "[DONE]")
                return "[DONE]";

            try
            {
                var chunk = JsonSerializer.Deserialize<DeepSeekChunk>(data,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Skipping malformed SSE chunk: {Data}", data);
                return null;
            }
        }

        /// <summary>
        /// Checks for known DeepSeek-specific error status codes and throws descriptive exceptions.
        /// </summary>
        private void HandleErrorStatusCodes(HttpResponseMessage response)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.PaymentRequired)
            {
                const string msg = "DeepSeek API: Payment Required. Please check your API key balance.";
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }
        }
    }
}