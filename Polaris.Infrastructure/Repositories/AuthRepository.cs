using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Polaris.Domain.Common.Models;
using Polaris.Domain.Entities;
using Polaris.Domain.Interfaces.Identity;
using Polaris.Domain.Interfaces.IRepositories;
using Polaris.Infrastructure.Identity;

namespace Polaris.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public AuthRepository(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // ===== Identity User Management =====

        public async Task<OperationResult> CreateIdentityUserAsync(LocalUser localUser, string password)
        {
            try
            {
                var appUser = new ApplicationUser
                {
                    Id = localUser.Id,
                    UserName = localUser.UserName,
                    Email = localUser.Email
                };

                var result = await _userManager.CreateAsync(appUser, password);

                if (!result.Succeeded)
                {
                    return OperationResult.Failure(
                        result.Errors.Select(e => e.Description).ToArray()
                    );
                }

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Failed to create identity user: {ex.Message}");
            }
        }

        public async Task<IApplicationUser?> GetIdentityUserByIdAsync(Guid userId)
        {
            return await _userManager.FindByIdAsync(userId.ToString());
        }

        public async Task<IApplicationUser?> GetIdentityUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<IApplicationUser?> GetIdentityUserByRefreshTokenAsync(string refreshToken)
        {
            return await _userManager.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        }

        public async Task<bool> IdentityUserExistsAsync(Guid userId)
        {
            return await _userManager.FindByIdAsync(userId.ToString()) != null;
        }

        public async Task DeleteIdentityUserAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, roles);
                }
                await _userManager.DeleteAsync(user);
            }
        }

        // ===== Password Operations =====

        public async Task<bool> CheckPasswordAsync(Guid userId, string password)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            return await _userManager.CheckPasswordAsync(user, password);
        }

        public async Task<OperationResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return OperationResult.Failure("User not found");

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (!result.Succeeded)
            {
                return OperationResult.Failure(
                    result.Errors.Select(e => e.Description).ToArray()
                );
            }

            return OperationResult.Success();
        }

        // ===== Role Operations =====

        public async Task<IList<string>> GetUserRolesAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return new List<string>();

            return await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> IsInRoleAsync(Guid userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            return await _userManager.IsInRoleAsync(user, role);
        }

        public async Task<OperationResult> AddToRoleAsync(Guid userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return OperationResult.Failure("User not found");

            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }

            var result = await _userManager.AddToRoleAsync(user, role);

            if (!result.Succeeded)
            {
                return OperationResult.Failure(
                    result.Errors.Select(e => e.Description).ToArray()
                );
            }

            return OperationResult.Success();
        }

        public async Task<OperationResult> RemoveFromRoleAsync(Guid userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return OperationResult.Failure("User not found");

            var result = await _userManager.RemoveFromRoleAsync(user, role);

            if (!result.Succeeded)
            {
                return OperationResult.Failure(
                    result.Errors.Select(e => e.Description).ToArray()
                );
            }

            return OperationResult.Success();
        }

        // ===== Token Operations =====

        public async Task UpdateRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiryTime)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null)
            {
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = expiryTime;
                await _userManager.UpdateAsync(user);
            }
        }

        public async Task ClearRefreshTokenAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await _userManager.UpdateAsync(user);
            }
        }

        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null) return false;

            return user.RefreshTokenExpiryTime > DateTime.UtcNow;
        }

        // ===== 2FA Operations (for future) =====

        public async Task<bool> IsTwoFactorEnabledAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            return user?.TwoFactorEnabled ?? false;
        }

        public async Task<string> GenerateTwoFactorTokenAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return string.Empty;

            return await _userManager.GenerateTwoFactorTokenAsync(user, "Authenticator");
        }

        public async Task<bool> VerifyTwoFactorTokenAsync(Guid userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            return await _userManager.VerifyTwoFactorTokenAsync(user, "Authenticator", token);
        }

        // ===== External Login Operations (for future) =====

        public async Task<IApplicationUser?> FindByExternalLoginAsync(string loginProvider, string providerKey)
        {
            var user = await _userManager.FindByLoginAsync(loginProvider, providerKey);
            return user;
        }

        public async Task AddExternalLoginAsync(Guid userId, string loginProvider, string providerKey)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null)
            {
                var userLoginInfo = new UserLoginInfo(loginProvider, providerKey, loginProvider);
                await _userManager.AddLoginAsync(user, userLoginInfo);
            }
        }

        public async Task<string> GenerateEmailConfirmationTokenAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return string.Empty;

            user.EmailConfirmationTokenSentAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return await _userManager.GenerateEmailConfirmationTokenAsync(user);
        }

        public async Task<bool> ConfirmEmailAsync(Guid userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            var result = await _userManager.ConfirmEmailAsync(user, token);
            return result.Succeeded;
        }

        public async Task<bool> IsEmailConfirmedAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            return await _userManager.IsEmailConfirmedAsync(user);
        }

        public async Task<string> GeneratePasswordResetTokenAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return string.Empty;

            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            return result.Succeeded;
        }

        public async Task<bool> ResendEmailConfirmationAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;

            if (await _userManager.IsEmailConfirmedAsync(user))
                return false;

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            // Send email with token
            return true;
        }

        public async Task SaveResetTokenAsync(string email, string numericToken)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return;

            user.ResetPasswordToken = numericToken;
            user.ResetPasswordTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
            user.ResePasswordTokenUsed = false;

            await _userManager.UpdateAsync(user);
        }

        public async Task<bool> ValidateResetTokenAsync(string email, string numericToken)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;

            return user.ResetPasswordToken == numericToken &&
                   user.ResePasswordTokenUsed == false &&
                   user.ResetPasswordTokenExpiresAt > DateTime.UtcNow;
        }

        public async Task MarkResetTokenAsUsedAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return;

            user.ResePasswordTokenUsed = true;
            await _userManager.UpdateAsync(user);
        }

        public async Task<bool> IsEmailConfirmationTokenValidAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;

            if (user.EmailConfirmationTokenSentAt.HasValue)
            {
                var timeSinceLastToken = DateTime.UtcNow - user.EmailConfirmationTokenSentAt.Value;
                return timeSinceLastToken.TotalHours < 24;
            }

            return false;
        }
    }
}