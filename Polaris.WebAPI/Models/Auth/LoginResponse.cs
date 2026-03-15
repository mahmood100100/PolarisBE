using Polaris.Application.Features.Auth.Commands.Login;

namespace Polaris.WebAPI.Models.Auth
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = new();
    }
}
