using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Domain.Interfaces.Identity
{
    public interface IApplicationUser
    {
        Guid Id { get; }
        string UserName { get; }
        string Email { get; }
        bool EmailConfirmed { get; }
        string? RefreshToken { get; }
        DateTime? RefreshTokenExpiryTime { get; }
        DateTime? LastLoginAt { get; set; }
        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordTokenExpiresAt { get; set; }
        public bool? ResePasswordTokenUsed { get; set; }
        public DateTime? EmailConfirmationTokenSentAt { get; set; }
    }
}
