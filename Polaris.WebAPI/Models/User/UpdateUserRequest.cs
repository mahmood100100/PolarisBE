using System.ComponentModel.DataAnnotations;

namespace Polaris.WebAPI.Models.User
{
    public class UpdateUserRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public IFormFile? ProfileImage { get; set; }

        public bool RemoveImage { get; set; }
    }
}
