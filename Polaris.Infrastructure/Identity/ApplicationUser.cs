using Microsoft.AspNetCore.Identity;
using Polaris.Domain.Interfaces.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser<Guid>, IApplicationUser
    {
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordTokenExpiresAt { get; set; }
        public bool? ResePasswordTokenUsed { get; set; }
        public DateTime? EmailConfirmationTokenSentAt { get; set; }
    }
}
