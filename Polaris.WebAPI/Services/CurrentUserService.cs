using Polaris.Application.Common.Interfaces;
using System.Security.Claims;

namespace Polaris.WebAPI.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                var claim = _httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.NameIdentifier)
                    ?? _httpContextAccessor.HttpContext?.User?
                    .FindFirst("id")
                    ?? _httpContextAccessor.HttpContext?.User?
                    .FindFirst("userId");

                if (claim != null && Guid.TryParse(claim.Value, out var userId))
                    return userId;

                return null;
            }
        }

        public string? UserName
        {
            get
            {
                return _httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.Name)?.Value
                    ?? _httpContextAccessor.HttpContext?.User?
                    .FindFirst("username")?.Value
                    ?? _httpContextAccessor.HttpContext?.User?
                    .FindFirst("name")?.Value;
            }
        }

        public string? Email
        {
            get
            {
                return _httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.Email)?.Value;
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                return _httpContextAccessor.HttpContext?.User?
                    .Identity?.IsAuthenticated ?? false;
            }
        }

        public bool IsAdmin
        {
            get
            {
                return _httpContextAccessor.HttpContext?.User?
                    .IsInRole("admin") ?? false;
            }
        }

        public string[] Roles
        {
            get
            {
                return _httpContextAccessor.HttpContext?.User?
                    .Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToArray() ?? Array.Empty<string>();
            }
        }

        public bool HasPermission(string permission)
        {
            return _httpContextAccessor.HttpContext?.User?
                .HasClaim(c => c.Type == "permission" && c.Value == permission) ?? false;
        }
    }
}
