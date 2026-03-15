using System.ComponentModel.DataAnnotations;

namespace Polaris.WebAPI.Models.Auth
{
    public class ResendResetTokenRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
