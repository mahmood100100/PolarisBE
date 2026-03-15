using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Application.Features.Auth.Commands.Login
{
    public class LoginResult
    {
        public bool Succeeded { get; set; }
        public string[]? Errors { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public UserDto? User { get; set; }
        public bool RequiresEmailConfirmation { get; set; }
        public string? EmailConfirmationStatus { get; set; }
        public string? Message { get; set; }
    }
}
