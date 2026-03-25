using Microsoft.Extensions.Logging;
using Polaris.Application.Common.Interfaces;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Polaris.Infrastructure.Services
{
    public class FirecrawlService : IFirecrawlService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FirecrawlService> _logger;

        public FirecrawlService(HttpClient httpClient, ILogger<FirecrawlService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string?> ScrapeUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                var requestBody = new { url = url };
                
                var response = await _httpClient.PostAsJsonAsync("v1/scrape", requestBody, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Firecrawl scraping failed for URL: {Url}. Status: {StatusCode}", url, response.StatusCode);
                    return null;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

                if (result.TryGetProperty("success", out var success) && success.GetBoolean() &&
                    result.TryGetProperty("data", out var data) && 
                    data.TryGetProperty("markdown", out var markdown))
                {
                    return markdown.GetString();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while scraping URL via Firecrawl: {Url}", url);
                return null;
            }
        }

        public async Task<string?> SearchWebAsync(string query, CancellationToken cancellationToken = default)
        {
            try
            {
                var requestBody = new { query = query };
                
                var response = await _httpClient.PostAsJsonAsync("v1/search", requestBody, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Firecrawl search failed for query: {Query}. Status: {StatusCode}", query, response.StatusCode);
                    return null;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

                if (result.TryGetProperty("success", out var success) && success.GetBoolean() &&
                    result.TryGetProperty("data", out var data) &&
                    data.ValueKind == JsonValueKind.Array)
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var item in data.EnumerateArray())
                    {
                        var title = item.TryGetProperty("title", out var t) ? t.GetString() : "No Title";
                        var url = item.TryGetProperty("url", out var u) ? u.GetString() : "No URL";
                        var desc = item.TryGetProperty("description", out var d) ? d.GetString() : "No Description";
                        
                        sb.AppendLine($"Title: {title}");
                        sb.AppendLine($"URL: {url}");
                        sb.AppendLine($"Description: {desc}");
                        sb.AppendLine("---");
                    }
                    return sb.ToString();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while searching web via Firecrawl: {Query}", query);
                return null;
            }
        }
    }
}
