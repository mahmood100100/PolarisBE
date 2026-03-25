using Hangfire;
using Microsoft.Extensions.Logging;
using Polaris.Application.Common.DTOs;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Entities;
using Polaris.Domain.Enums;
using Polaris.Domain.Interfaces.IRepositories;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Polaris.Infrastructure.Services
{
    /// <summary>
    /// Background job processor for AI Chat.
    /// Handles the real-time AI response streaming within a Hangfire background job,
    /// so the user can easily disconnect and reconnect (refresh the page) using the JobId.
    /// </summary>
    public class ChatJobProcessor : IChatJobProcessor
    {
        private readonly IGenerationJobRepository _jobRepository;
        private readonly IUnitOfWork _uow;
        private readonly IDeepSeekAIService _deepSeekService;
        private readonly IGenerationStreamManager _streamManager;
        private readonly IFirecrawlService _firecrawlService;
        private readonly ILogger<ChatJobProcessor> _logger;

        public ChatJobProcessor(
            IGenerationJobRepository jobRepository,
            IUnitOfWork uow,
            IDeepSeekAIService deepSeekService,
            IGenerationStreamManager streamManager,
            IFirecrawlService firecrawlService,
            ILogger<ChatJobProcessor> logger)
        {
            _jobRepository = jobRepository;
            _uow = uow;
            _deepSeekService = deepSeekService;
            _streamManager = streamManager;
            _firecrawlService = firecrawlService;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        public async Task ProcessChatAsync(Guid jobId, Guid conversationId, string message, Guid userId)
        {
            // 1. Fetch the Job
            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                _logger.LogError("Chat job {JobId} not found", jobId);
                return;
            }

            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            var fullResult = new StringBuilder();

            // 2. Open Stream Channel
            _streamManager.CreateChannel(jobId);

            try
            {
                // Update job state
                job.Status = "Processing";
                job.StartedAt = DateTime.UtcNow;
                job.Progress = 5;
                await _jobRepository.UpdateAsync(job);

                // 3. Create a placeholder Assistant Message in the database
                var assistantMessage = new Message
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversationId,
                    Content = "",
                    Role = MessageRole.Assistant,
                    SentAt = DateTime.UtcNow,
                    Status = MessageStatus.Pending // Indicate it's still being typed
                };
                await _uow.Messages.CreateAsync(assistantMessage);
                await _uow.CompleteAsync();

                // 4. Load Conversation History (up to 20 messages for context)
                var history = await _uow.Messages.GetLastNMessagesAsync(conversationId, 20);
                
                // Exclude the current empty assistant message from sending to AI
                var promptHistory = history
                    .Where(m => m.Id != assistantMessage.Id && m.Content.Length > 0)
                    .Select(m => new ChatMessageDto
                    {
                        Role = m.Role.ToString().ToLower(),
                        Content = m.Content
                    }).ToList();

                // ─── SYSTEM CONTEXT INJECTION ───
                promptHistory.Insert(0, new ChatMessageDto
                {
                    Role = "system",
                    Content = $"You are Polaris AI. " +
                              $"CRITICAL RULE: You DO have access to real-time clock. " +
                              $"The current local time is: {DateTime.Now:f}. " +
                              $"If the user asks for the time or date, YOU MUST give them this exact time directly. " +
                              $"DO NOT say you are an AI. DO NOT apologize. DO NOT say you cannot access the system time. " +
                              $"أنت مساعد ذكي اسمك بولاريس. الوقت والتاريخ الفعلي الآن هو {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}. إياك أن تعتذر وتقول أنك لا تملك الوقت! أجب بالوقت فوراً."
                });
                // ────────────────────────────────

                // ─── AGENT/TOOL CALLING: Let AI Decide if it needs to use any tools ───
                _streamManager.WriteAsync(jobId, "Thinking...", cts.Token).GetAwaiter().GetResult();
                var toolCall = await _deepSeekService.AnalyzeForToolsAsync(promptHistory, cts.Token);
                
                if (toolCall.HasValue)
                {
                    var (toolName, argumentsJson) = toolCall.Value;
                    string? toolResult = null;
                    string actionMessage = "";

                    using var argsDoc = System.Text.Json.JsonDocument.Parse(argumentsJson);

                    switch (toolName)
                    {
                        case "scrape_website":
                            if (argsDoc.RootElement.TryGetProperty("url", out var urlElement))
                            {
                                var url = urlElement.GetString();
                                _logger.LogInformation("Agent decided to scrape URL: {Url}", url);
                                actionMessage = $"\n\n*Reading website content from {url}...*\n\n";
                                _streamManager.WriteAsync(jobId, actionMessage, cts.Token).GetAwaiter().GetResult();
                                toolResult = await _firecrawlService.ScrapeUrlAsync(url, cts.Token);
                            }
                            break;

                        case "search_web":
                            if (argsDoc.RootElement.TryGetProperty("query", out var queryElement))
                            {
                                var query = queryElement.GetString();
                                _logger.LogInformation("Agent decided to search web for: {Query}", query);
                                actionMessage = $"\n\n*Searching the web for '{query}'...*\n\n";
                                _streamManager.WriteAsync(jobId, actionMessage, cts.Token).GetAwaiter().GetResult();
                                toolResult = await _firecrawlService.SearchWebAsync(query, cts.Token);
                            }
                            break;
                    }

                    if (!string.IsNullOrEmpty(toolResult))
                    {
                        var lastUserMessage = promptHistory.LastOrDefault(m => m.Role == "user");
                        if (lastUserMessage != null)
                        {
                            // We inject the tool result back into the prompt history
                            lastUserMessage.Content = $"{lastUserMessage.Content}\n\n" +
                                                      $"[TOOL CALL RESULT]\n" +
                                                      $"I executed the '{toolName}' tool. Here are the true, live results:\n" +
                                                      $"{toolResult}\n" +
                                                      $"--- End of Tool Results ---\n" +
                                                      $"Please use this extracted data to answer the user's question accurately.";
                        }
                    }
                }
                // ────────────────────────────────────────────────────────────

                var chunkCounter = 0;

                // 5. Stream the Response
                await foreach (var chunk in _deepSeekService.StreamChatAsync(promptHistory).WithCancellation(cts.Token))
                {
                    fullResult.Append(chunk);
                    chunkCounter++;

                    // Push chunk to connected SSE clients
                    await _streamManager.WriteAsync(jobId, chunk, cts.Token);

                    // Periodically save progress to DB (every 3 chunks)
                    if (chunkCounter % 3 == 0)
                    {
                        var content = fullResult.ToString();
                        
                        // Save Job Progress
                        job.Result = content;
                        job.Progress = Math.Min(90, 10 + (chunkCounter / 2));
                        job.LastUpdatedAt = DateTime.UtcNow;
                        await _jobRepository.UpdateAsync(job);

                        // Save Message chunk
                        assistantMessage.Content = content;
                        await _uow.Messages.UpdateAsync(assistantMessage);
                        await _uow.CompleteAsync();
                    }

                    // Check Cancellation request (every 5 chunks)
                    if (chunkCounter % 5 == 0)
                    {
                        var freshJob = await _jobRepository.GetByIdAsync(jobId);
                        if (freshJob?.UserIntent == "cancelled")
                        {
                            job.Status = "Cancelled";
                            job.Result = fullResult.ToString();
                            job.CompletedAt = DateTime.UtcNow;
                            await _jobRepository.UpdateAsync(job);

                            assistantMessage.Content = fullResult.ToString();
                            assistantMessage.Status = MessageStatus.Completed;
                            await _uow.Messages.UpdateAsync(assistantMessage);
                            await _uow.CompleteAsync();
                            
                            _streamManager.Complete(jobId);
                            return;
                        }
                    }
                }

                // 6. Complete Job & Message
                var finalContent = fullResult.ToString();

                job.Status = "Completed";
                job.Result = finalContent;
                job.Progress = 100;
                job.CompletedAt = DateTime.UtcNow;
                job.LastUpdatedAt = DateTime.UtcNow;
                await _jobRepository.UpdateAsync(job);

                assistantMessage.Content = finalContent;
                assistantMessage.Status = MessageStatus.Completed;
                await _uow.Messages.UpdateAsync(assistantMessage);
                await _uow.CompleteAsync();

                _streamManager.Complete(jobId);
            }
            catch (OperationCanceledException)
            {
                job.Status = "Failed";
                job.Error = "Job timed out after 5 minutes";
                job.CompletedAt = DateTime.UtcNow;
                await _jobRepository.UpdateAsync(job);

                _streamManager.Complete(jobId, new OperationCanceledException("Timed out"));
            }
            catch (Exception ex)
            {
                job.Status = "Failed";
                job.Error = ex.Message;
                job.CompletedAt = DateTime.UtcNow;
                await _jobRepository.UpdateAsync(job);

                _streamManager.Complete(jobId, ex);
                throw; // Retry by Hangfire
            }
        }
    }
}
