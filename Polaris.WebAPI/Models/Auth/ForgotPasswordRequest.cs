using System.ComponentModel.DataAnnotations;

namespace Polaris.WebAPI.Models.Auth
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
