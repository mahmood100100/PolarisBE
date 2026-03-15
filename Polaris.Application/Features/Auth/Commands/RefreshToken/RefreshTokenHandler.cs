using MediatR;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Interfaces.IRepositories;

namespace Polaris.Application.Features.Auth.Commands.RefreshToken
{
    public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;

        public RefreshTokenHandler(IUnitOfWork unitOfWork, ITokenService tokenService)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
        }

        public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate refresh token
                var isValid = await _unitOfWork.Auth.ValidateRefreshTokenAsync(request.RefreshToken);
                if (!isValid)
                {
                    return new RefreshTokenResult
                    {
                        Succeeded = false,
                        Errors = new[] { "Invalid or expired refresh token" }
                    };
                }

                // Get user by refresh token
                var appUser = await _unitOfWork.Auth.GetIdentityUserByRefreshTokenAsync(request.RefreshToken);
                if (appUser == null)
                {
                    return new RefreshTokenResult
                    {
                        Succeeded = false,
                        Errors = new[] { "User not found" }
                    };
                }

                // Get local user
                var localUser = await _unitOfWork.Users.GetByIdAsync(appUser.Id);
                if (localUser == null)
                {
                    return new RefreshTokenResult
                    {
                        Succeeded = false,
                        Errors = new[] { "User profile not found" }
                    };
                }

                // Get user roles
                var roles = await _unitOfWork.Auth.GetUserRolesAsync(appUser.Id);

                // 5. Generate new tokens (rotation)
                var newAccessToken = _tokenService.GenerateAccessToken(localUser, roles);
                var newRefreshToken = _tokenService.GenerateRefreshToken();
                var expiresAt = DateTime.UtcNow.AddMinutes(15);

                // Update refresh token in database
                await _unitOfWork.Auth.UpdateRefreshTokenAsync(
                    appUser.Id,
                    newRefreshToken,
                    DateTime.UtcNow.AddDays(7));

                await _unitOfWork.CompleteAsync(cancellationToken);

                return new RefreshTokenResult
                {
                    Succeeded = true,
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = expiresAt
                };
            }
            catch (Exception ex)
            {
                return new RefreshTokenResult
                {
                    Succeeded = false,
                    Errors = new[] { $"Refresh token failed: {ex.Message}" }
                };
            }
        }
    }
}