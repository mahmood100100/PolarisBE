using Microsoft.AspNetCore.Mvc;
using Polaris.Application.Common.Interfaces;

namespace Polaris.WebAPI.Services
{
    public class LinkGeneratorService : ILinkGeneratorService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LinkGenerator _linkGenerator;

        public LinkGeneratorService(
            IHttpContextAccessor httpContextAccessor,
            LinkGenerator linkGenerator)
        {
            _httpContextAccessor = httpContextAccessor;
            _linkGenerator = linkGenerator;
        }

        public string GenerateEmailConfirmationLink(Guid userId, string token)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                throw new InvalidOperationException("No HTTP context available");

            // Encode token for URL
            var encodedToken = Uri.EscapeDataString(token);

            return _linkGenerator.GetUriByAction(
                httpContext,
                action: "ConfirmEmail",
                controller: "Auth",
                values: new { userId, token = encodedToken }
            ) ?? throw new InvalidOperationException("Could not generate link");
        }

        public string GeneratePasswordResetLink(string email, string token)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                throw new InvalidOperationException("No HTTP context available");

            var encodedToken = Uri.EscapeDataString(token);
            var encodedEmail = Uri.EscapeDataString(email);

            return _linkGenerator.GetUriByAction(
                httpContext,
                action: "ResetPassword",
                controller: "Auth",
                values: new { email = encodedEmail, token = encodedToken }
            ) ?? throw new InvalidOperationException("Could not generate link");
        }
    }
}