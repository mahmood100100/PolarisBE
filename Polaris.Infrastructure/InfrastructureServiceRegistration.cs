using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Interfaces.IRepositories;
using Polaris.Infrastructure.Data;
using Polaris.Infrastructure.ExternalServices;
using Polaris.Infrastructure.Helpers;
using Polaris.Infrastructure.Repositories;
using Polaris.Infrastructure.Services;

namespace Polaris.Infrastructure
{
    /// <summary>
    /// Infrastructure layer DI registration.
    /// Registers all infrastructure services: database context, repositories,
    /// external API clients, background job services, and streaming infrastructure.
    /// </summary>
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services)
        {
            // Load connection strings from environment variables
            var connectionString = EnvironmentHelper.GetEnvironmentVariable("ConnectionStrings__PostgresConnection");
            var hangFireConnectionString = EnvironmentHelper.GetEnvironmentVariable("ConnectionStrings__HangfireConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must be provided in environment variables");

            if (string.IsNullOrWhiteSpace(hangFireConnectionString))
                throw new ArgumentException("Hangfire connection string must be provided");

            // ─── Database Context ───────────────────────────────────────────
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // ─── Repositories ───────────────────────────────────────────────
            // UnitOfWork is the single entry point for all repositories.
            // Individual repositories are lazily initialized inside UoW and
            // should NOT be registered separately in DI.
            //
            // Exception: IGenerationJobRepository is also registered standalone
            // because GenerationJobProcessor runs inside Hangfire (separate DI scope)
            // and cannot access UoW directly during long-running background work.
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IGenerationJobRepository, GenerationJobRepository>();

            // ─── External Services ──────────────────────────────────────────
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IEmailService, EmailService>();

            // ─── Generation Pipeline Services ───────────────────────────────
            // The job processor runs inside Hangfire workers and streams AI output
            services.AddScoped<IChatJobProcessor, ChatJobProcessor>();

            // Stream Manager MUST be Singleton to share channels between
            // Hangfire background workers and HTTP streaming endpoints.
            // Both run in different DI scopes but need to reference the same
            // in-memory channel instances for real-time communication.
            services.AddSingleton<IGenerationStreamManager, GenerationStreamManager>();

            // ─── Hangfire Configuration ─────────────────────────────────────
            services.AddHangfire(config =>
               config.UsePostgreSqlStorage(options =>
                  options.UseNpgsqlConnection(hangFireConnectionString)));

            services.AddHangfireServer();

            // Background job service wraps Hangfire for the Application layer
            services.AddScoped<IBackgroundJobService, BackgroundJobService>();

            // ─── DeepSeek AI HTTP Client ────────────────────────────────────
            // Configured with base address, timeout, and API key authentication
            services.AddHttpClient<IDeepSeekAIService, DeepSeekService>(client =>
            {
                client.BaseAddress = new Uri("https://api.deepseek.com");
                client.Timeout = TimeSpan.FromMinutes(10);
                var apiKey = EnvironmentHelper.GetEnvironmentVariable("DeepSeek__ApiKey");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            });

            // ─── Firecrawl AI Web Scraper ───────────────────────────────────
            services.AddHttpClient<IFirecrawlService, FirecrawlService>(client =>
            {
                client.BaseAddress = new Uri("https://api.firecrawl.dev/");
                client.Timeout = TimeSpan.FromMinutes(2); // Scraping might take a bit longer
                var apiKey = EnvironmentHelper.GetEnvironmentVariable("Firecrawl__ApiKey");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            });

            return services;
        }
    }
}