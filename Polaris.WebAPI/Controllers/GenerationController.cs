using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Polaris.Application.Common.Interfaces;
using Polaris.WebAPI.Models.AiGeneration;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Polaris.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GenerationController : ControllerBase
    {
        private readonly IBackgroundJobClient _hangfire;
        private readonly IAIGenerationService _aiService;
        private readonly ILogger<GenerationController> _logger;

        public GenerationController(
            IBackgroundJobClient hangfire,
            IAIGenerationService aiService,
            ILogger<GenerationController> logger)
        {
            _hangfire = hangfire;
            _aiService = aiService;
            _logger = logger;
        }

        [HttpPost("start")]
        public IActionResult StartGeneration([FromBody] GenerationRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var userGuid = Guid.Parse(userId);

            var jobId = _hangfire.Enqueue(() =>
                _aiService.GenerateCodeAsync(request.Prompt, userGuid));

            _logger.LogInformation($"Job {jobId} started for user {userId}");

            return Ok(new
            {
                jobId = jobId,
                message = "تم بدء توليد الكود في الخلفية",
                status = "processing"
            });
        }
    }
}