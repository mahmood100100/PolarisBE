namespace Polaris.WebAPI.Models.Auth
{
    public class SocialLoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
    }
}
