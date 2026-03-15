using Microsoft.Extensions.Logging;
using Polaris.Application.Common.Interfaces;
using System;
using System.Threading.Tasks;

namespace Polaris.Infrastructure.Services
{
    public class AIGenerationService : IAIGenerationService
    {
        private readonly ILogger<AIGenerationService> _logger;

        public AIGenerationService(ILogger<AIGenerationService> logger)
        {
            _logger = logger;
        }

        public async Task<string> GenerateCodeAsync(string prompt, Guid userId)
        {
            _logger.LogInformation($"بدأ توليد كود للمستخدم {userId} بالطلب: {prompt}");

            await Task.Delay(5000);

            var result = $@"
// كود تم توليده للمستخدم {userId}
function helloWorld() {{
    console.log('Hello from {prompt}');
}}";

            _logger.LogInformation($"اكتمل توليد الكود للمستخدم {userId}");

            return result;
        }
    }
}