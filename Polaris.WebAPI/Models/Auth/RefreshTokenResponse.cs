namespace Polaris.WebAPI.Models.Auth
{
    public class RefreshTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
