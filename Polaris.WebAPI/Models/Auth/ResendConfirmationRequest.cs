using System.ComponentModel.DataAnnotations;

namespace Polaris.WebAPI.Models.Auth
{
    public class ResendConfirmationRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
