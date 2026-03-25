using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Polaris.Application.Features.Conversations.Commands.CreateConversation;
using Polaris.Application.Features.Conversations.Commands.StartChatMessage;
using Polaris.Application.Features.Conversations.Queries.GetConversationMessages;
using Polaris.Application.Features.Conversations.Queries.GetConversations;
using Polaris.Application.Features.Conversations.Queries.StreamChatJobById;
using System.Security.Claims;
using System.Text.Json;

namespace Polaris.WebAPI.Controllers
{
    /// <summary>
    /// Controller for handling AI chat conversations.
    /// Supports managing conversation history and real-time streaming of AI responses.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IMediator mediator, ILogger<ChatController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════════════════
        // Conversations Management
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Creates a new conversation.</summary>
        [HttpPost("conversations")]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationCommand command)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            command.UserId = userId.Value;
            var response = await _mediator.Send(command);
            
            return Ok(response);
        }

        /// <summary>Gets all conversations for a user, optionally filtered by ProjectId.</summary>
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations([FromQuery] Guid? projectId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var query = new GetConversationsQuery 
            { 
                UserId = userId.Value, 
                ProjectId = projectId 
            };
            
            var conversations = await _mediator.Send(query);
            return Ok(conversations);
        }

        /// <summary>Deletes a conversation and all its messages.</summary>
        [HttpDelete("conversations/{conversationId}")]
        public async Task<IActionResult> DeleteConversation(Guid conversationId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var command = new Polaris.Application.Features.Conversations.Commands.DeleteConversation.DeleteConversationCommand 
            { 
                ConversationId = conversationId, 
                UserId = userId.Value 
            };
            
            var success = await _mediator.Send(command);
            
            if (!success)
            {
                return NotFound(new { message = "Conversation not found or access denied." });
            }
            
            return Ok(new { message = "Conversation deleted successfully." });
        }

        /// <summary>Gets the full message history for a given conversation.</summary>
        [HttpGet("conversations/{conversationId}/messages")]
        public async Task<IActionResult> GetConversationMessages(Guid conversationId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var query = new GetConversationMessagesQuery 
            { 
                ConversationId = conversationId, 
                UserId = userId.Value 
            };
            
            var messages = await _mediator.Send(query);
            return Ok(messages);
        }

        // ═══════════════════════════════════════════════════════════════════
        // Real-Time Chat Streaming (Background Job + SSE)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Step 1: Saves the user message and starts a background Hangfire job to generate the AI response.
        /// Returns a JobId which the client uses to connect to the stream.
        /// </summary>
        [HttpPost("messages/start")]
        public async Task<IActionResult> StartChatMessage([FromBody] StartChatMessageCommand command)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            command.UserId = userId.Value;
            var response = await _mediator.Send(command);
            
            return Ok(response);
        }

        /// <summary>
        /// Step 2: The client connects here using the JobId to stream the AI response word-by-word.
        /// If the page refreshes, the client can reconnect here and the DB will replay any missed words.
        /// </summary>
        [HttpGet("messages/stream/{jobId}")]
        public async Task StreamChatJob(Guid jobId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                Response.StatusCode = 401;
                return;
            }

            ConfigureSSEResponse();
            var cancellationToken = HttpContext.RequestAborted;

            try
            {
                // We reuse the exact same stream reader (StreamJobByIdQuery) from the Generation namespace
                // because the underlying streaming infrastructure (In-Memory Channels + DB Buffer) is identical.
                var result = await _mediator.Send(new StreamJobByIdQuery
                {
                    JobId = jobId,
                    UserId = userId.Value
                }, cancellationToken);

                if (!result.Found)
                {
                    await SendSSE(new { error = result.Error ?? "Job not found" }, cancellationToken);
                    return;
                }

                // Iterate the async stream of events and write each as SSE
                await foreach (var streamEvent in result.Events!.WithCancellation(cancellationToken))
                {
                    await WriteStreamEvent(streamEvent, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Chat stream cancelled by client for JobId: {JobId}", jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat streaming error for JobId: {JobId}", jobId);
                try
                {
                    await SendSSE(new { error = "An internal error occurred during streaming." }, cancellationToken);
                }
                catch { /* The client may have disconnected already */ }
            }
        }

        /// <summary>
        /// Gets all active (Processing/Pending) chat jobs for the current user.
        /// The Frontend calls this on page refresh to discover which conversations are currently typing
        /// so it can automatically reconnect to their SSE streams!
        /// </summary>
        [HttpGet("messages/active")]
        public async Task<IActionResult> GetActiveChatJobs()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var query = new Polaris.Application.Features.Conversations.Queries.GetActiveChatJobs.GetActiveChatJobsQuery 
            { 
                UserId = userId.Value 
            };
            
            var activeJobs = await _mediator.Send(query);
            return Ok(activeJobs);
        }

        // ═══════════════════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════════════════

        private Guid? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim)) return null;
            return Guid.Parse(claim);
        }

        private void ConfigureSSEResponse()
        {
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";
            HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        }

        private async Task SendSSE(object data, CancellationToken ct, bool raw = false)
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            string line = raw && data is string rawStr 
                ? $"data: {rawStr}\n\n" 
                : $"data: {JsonSerializer.Serialize(data, options)}\n\n";

            await Response.WriteAsync(line, ct);
            await Response.Body.FlushAsync(ct);
        }

        /// <summary>
        /// Converts the common stream events from the CQRS handler into structured JSON for the frontend.
        /// </summary>
        private async Task WriteStreamEvent(StreamEvent streamEvent, CancellationToken ct)
        {
            if (streamEvent.IsRaw && streamEvent.RawValue != null)
            {
                await SendSSE(streamEvent.RawValue, ct, raw: true);
                return;
            }

            switch (streamEvent.Type)
            {
                case StreamEventType.Buffer:
                    await SendSSE(new
                    {
                        type = "buffer",
                        content = streamEvent.Content,
                        status = streamEvent.Status,
                        progress = streamEvent.Progress
                    }, ct);
                    break;

                case StreamEventType.Chunk:
                    await SendSSE(new
                    {
                        content = streamEvent.Content,
                        status = streamEvent.Status
                    }, ct);
                    break;

                case StreamEventType.Error:
                    await SendSSE(new
                    {
                        type = "error",
                        error = streamEvent.Error,
                        status = streamEvent.Status,
                        partialContent = streamEvent.PartialContent
                    }, ct);
                    break;

                case StreamEventType.Heartbeat:
                    await SendSSE(new
                    {
                        type = "heartbeat",
                        status = streamEvent.Status,
                        progress = streamEvent.Progress,
                        waitingTime = streamEvent.WaitingTime
                    }, ct);
                    break;

                case StreamEventType.Done:
                    await SendSSE("[DONE]", ct, raw: true);
                    break;
            }
        }
    }
}
