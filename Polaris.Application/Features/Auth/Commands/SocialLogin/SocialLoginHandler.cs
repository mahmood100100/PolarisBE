using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Polaris.Application.Common.Exceptions;
using Polaris.Application.Common.Interfaces;
using Polaris.Application.Features.Auth.Commands.Login;
using Polaris.Domain.Entities;
using Polaris.Domain.Interfaces.Identity;
using Polaris.Domain.Interfaces.IRepositories;

namespace Polaris.Application.Features.Auth.Commands.SocialLogin
{
    public class SocialLoginHandler : IRequestHandler<SocialLoginCommand, SocialLoginResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly ILogger<SocialLoginHandler> _logger;

        public SocialLoginHandler(
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            IMapper mapper,
            ILogger<SocialLoginHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<SocialLoginResult> Handle(SocialLoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Basic validation
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.ProviderId))
                {
                    return new SocialLoginResult
                    {
                        Succeeded = false,
                        Errors = new[] { "Email and ProviderId are required" }
                    };
                }

                // 2. First, try to find user by external login (provider + providerId)
                var userByExternalLogin = await _unitOfWork.Auth.FindByExternalLoginAsync(request.Provider, request.ProviderId);

                if (userByExternalLogin != null)
                {
                    _logger.LogInformation("User found by external login {Provider} with ID {ProviderId}", request.Provider, request.ProviderId);

                    // Get local user
                    var existingLocalUser = await _unitOfWork.Users.GetByIdAsync(userByExternalLogin.Id);
                    if (existingLocalUser == null)
                    {
                        _logger.LogError("Inconsistent state: Identity user {UserId} has no LocalUser", userByExternalLogin.Id);
                        return new SocialLoginResult
                        {
                            Succeeded = false,
                            Errors = new[] { "User account is in an inconsistent state" }
                        };
                    }

                    // Update last login
                    existingLocalUser.LastLoginAt = DateTime.UtcNow;
                    _unitOfWork.GetGenericRepository<LocalUser>().Update(existingLocalUser);
                    await _unitOfWork.CompleteAsync(cancellationToken);

                    // Complete login process
                    return await CompleteSocialLogin(userByExternalLogin, existingLocalUser, request, cancellationToken);
                }

                // 3. If not found by external login, try by email
                var existingIdentityUser = await _unitOfWork.Auth.GetIdentityUserByEmailAsync(request.Email);

                LocalUser? localUser = null;
                IApplicationUser? appUser = existingIdentityUser;

                if (existingIdentityUser == null)
                {
                    // 4. New user - create both LocalUser and IdentityUser
                    _logger.LogInformation("Creating new user from {Provider} with email {Email}", request.Provider, request.Email);

                    localUser = new LocalUser
                    {
                        Id = Guid.NewGuid(),
                        FullName = request.Name ?? request.Email.Split('@')[0],
                        UserName = request.Email.Split('@')[0],
                        Email = request.Email,
                        ImageUrl = string.Empty,
                        CreatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow
                    };

                    // Use transaction to ensure consistency
                    return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                    {
                        // Create local user
                        await _unitOfWork.Users.AddAsync(localUser);

                        // ✅ Create identity user with random password AND confirm email automatically
                        var createResult = await _unitOfWork.Auth.CreateIdentityUserAsync(localUser, GenerateRandomPassword());

                        if (!createResult.Succeeded)
                        {
                            throw new ValidationException($"Failed to create identity user: {string.Join(", ", createResult.Errors ?? new[] { "Unknown error" })}");
                        }

                        // ✅ Confirm email immediately for social login users
                        var emailConfirmationResult = await _unitOfWork.Auth.ConfirmEmailAsync(localUser.Id, "dummy-token");
                        if (!emailConfirmationResult)
                        {
                            _logger.LogWarning("Failed to auto-confirm email for social user {UserId}", localUser.Id);
                        }

                        // Add to default role
                        var roleResult = await _unitOfWork.Auth.AddToRoleAsync(localUser.Id, "user");

                        if (!roleResult.Succeeded)
                        {
                            throw new ValidationException($"Failed to assign role: {string.Join(", ", roleResult.Errors ?? new[] { "Unknown error" })}");
                        }

                        // Link external login to user
                        await _unitOfWork.Auth.AddExternalLoginAsync(
                            localUser.Id,
                            request.Provider,
                            request.ProviderId
                        );

                        // Get created identity user
                        appUser = await _unitOfWork.Auth.GetIdentityUserByIdAsync(localUser.Id);
                        if (appUser == null)
                        {
                            throw new ValidationException("Failed to retrieve created identity user");
                        }

                        _logger.LogInformation("Successfully created new user {UserId} with external login {Provider} and email auto-confirmed", localUser.Id, request.Provider);

                        return await CompleteSocialLogin(appUser, localUser, request, cancellationToken);
                    });
                }
                else
                {
                    // 5. Existing user found by email - link external login and proceed
                    _logger.LogInformation("Existing user found with email {Email}, linking external login {Provider}", request.Email, request.Provider);

                    localUser = await _unitOfWork.Users.GetByIdAsync(existingIdentityUser.Id);

                    if (localUser == null)
                    {
                        _logger.LogError("Inconsistent state: Identity user {UserId} has no LocalUser", existingIdentityUser.Id);
                        return new SocialLoginResult
                        {
                            Succeeded = false,
                            Errors = new[] { "User account is in an inconsistent state" }
                        };
                    }

                    // Link external login to existing user (if not already linked)
                    try
                    {
                        await _unitOfWork.Auth.AddExternalLoginAsync(
                            existingIdentityUser.Id,
                            request.Provider,
                            request.ProviderId
                        );
                        _logger.LogInformation("Linked external login {Provider} with ID {ProviderId} to existing user {UserId}",
                            request.Provider, request.ProviderId, existingIdentityUser.Id);
                    }
                    catch (Exception ex)
                    {
                        // If already linked, just log warning and continue
                        _logger.LogWarning(ex, "External login {Provider} may already be linked to user {UserId}",
                            request.Provider, existingIdentityUser.Id);
                    }

                    // Update last login
                    localUser.LastLoginAt = DateTime.UtcNow;
                    _unitOfWork.GetGenericRepository<LocalUser>().Update(localUser);
                    await _unitOfWork.CompleteAsync(cancellationToken);

                    return await CompleteSocialLogin(existingIdentityUser, localUser, request, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Social login failed for provider {Provider} with email {Email}", request.Provider, request.Email);
                return new SocialLoginResult
                {
                    Succeeded = false,
                    Errors = new[] { $"Social login failed: {ex.Message}" }
                };
            }
        }

        private async Task<SocialLoginResult> CompleteSocialLogin(
            IApplicationUser appUser,
            LocalUser localUser,
            SocialLoginCommand request,
            CancellationToken cancellationToken)
        {
            // Get user roles
            var roles = await _unitOfWork.Auth.GetUserRolesAsync(appUser.Id);

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(localUser, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(15);
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            // Update refresh token
            await _unitOfWork.Auth.UpdateRefreshTokenAsync(
                appUser.Id,
                refreshToken,
                refreshTokenExpiry);

            await _unitOfWork.CompleteAsync(cancellationToken);

            // Map to DTO - LocalUser doesn't need EmailConfirmed field
            var userDto = _mapper.Map<UserDto>(localUser);
            userDto.Roles = roles.ToList();

            return new SocialLoginResult
            {
                Succeeded = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = userDto
            };
        }

        private string GenerateRandomPassword()
        {
            var guid = Guid.NewGuid().ToString();
            return $"P@ssw0rd_{guid.Substring(0, 8)}!";
        }
    }
}