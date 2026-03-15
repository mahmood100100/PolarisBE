using Polaris.Domain.Common.Models;
using Polaris.Domain.Entities;
using Polaris.Domain.Interfaces.Identity;

namespace Polaris.Domain.Interfaces.IRepositories
{
    public interface IAuthRepository
    {
        // ===== Identity User Management =====
        Task<OperationResult> CreateIdentityUserAsync(LocalUser localUser, string password);
        Task<IApplicationUser?> GetIdentityUserByIdAsync(Guid userId);
        Task<IApplicationUser?> GetIdentityUserByEmailAsync(string email);
        Task<IApplicationUser?> GetIdentityUserByRefreshTokenAsync(string refreshToken);
        Task<bool> IdentityUserExistsAsync(Guid userId);
        Task DeleteIdentityUserAsync(Guid userId);

        // ===== Email Confirmation =====
        Task<string> GenerateEmailConfirmationTokenAsync(Guid userId);
        Task<bool> ConfirmEmailAsync(Guid userId, string token);
        Task<bool> IsEmailConfirmedAsync(Guid userId);
        Task<bool> ResendEmailConfirmationAsync(string email);

        // ===== Password Operations =====
        Task<bool> CheckPasswordAsync(Guid userId, string password);
        Task<OperationResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
        Task<string> GeneratePasswordResetTokenAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);

        // ===== Role Operations =====
        Task<IList<string>> GetUserRolesAsync(Guid userId);
        Task<bool> IsInRoleAsync(Guid userId, string role);
        Task<OperationResult> AddToRoleAsync(Guid userId, string role);
        Task<OperationResult> RemoveFromRoleAsync(Guid userId, string role);

        // ===== Token Operations =====
        Task UpdateRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiryTime);
        Task ClearRefreshTokenAsync(Guid userId);
        Task<bool> ValidateRefreshTokenAsync(string refreshToken);

        // ===== 2FA (for future) =====
        Task<bool> IsTwoFactorEnabledAsync(Guid userId);
        Task<string> GenerateTwoFactorTokenAsync(Guid userId);
        Task<bool> VerifyTwoFactorTokenAsync(Guid userId, string token);

        // ===== External Logins (for future) =====
        Task<IApplicationUser?> FindByExternalLoginAsync(string loginProvider, string providerKey);
        Task AddExternalLoginAsync(Guid userId, string loginProvider, string providerKey);

        Task SaveResetTokenAsync(string email, string numericToken);
        Task<bool> ValidateResetTokenAsync(string email, string numericToken);
        Task MarkResetTokenAsUsedAsync(string email);
        Task<bool> IsEmailConfirmationTokenValidAsync(string email);
    }
}