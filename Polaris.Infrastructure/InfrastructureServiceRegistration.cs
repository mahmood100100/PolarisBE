using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Interfaces.IRepositories;
using Polaris.Infrastructure.Data;
using Polaris.Infrastructure.ExternalServices;
using Polaris.Infrastructure.Repositories;
using Polaris.Infrastructure.Services;
using Polaris.Infrastructure.Helpers;
using Hangfire;
using Hangfire.PostgreSql;

namespace Polaris.Infrastructure
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services)
        {
            // Load connection string from environment variables
            var connectionString = EnvironmentHelper.GetEnvironmentVariable("ConnectionStrings__PostgresConnection");
            var hangFireConnectionString = EnvironmentHelper.GetEnvironmentVariable("ConnectionStrings__HangfireConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must be provided in environment variables");

            if (string.IsNullOrWhiteSpace(hangFireConnectionString))
                throw new ArgumentException("Hangfire connection string must be provided");

            // Database Context
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Repositories and Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAuthRepository, AuthRepository>();

            // External Services
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IAIGenerationService, AIGenerationService>();

            // Hangfire Configuration
            services.AddHangfire(config =>
               config.UsePostgreSqlStorage(options =>
                  options.UseNpgsqlConnection(hangFireConnectionString)));

            services.AddHangfireServer();

            return services;
        }
    }
}