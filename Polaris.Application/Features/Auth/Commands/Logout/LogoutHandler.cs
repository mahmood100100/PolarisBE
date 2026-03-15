using MediatR;
using Polaris.Application.Common.Exceptions;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Interfaces.IRepositories;

namespace Polaris.Application.Features.Auth.Commands.Logout
{
    public class LogoutHandler : IRequestHandler<LogoutCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public LogoutHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            // Check if user is authenticated
            if (!_currentUserService.IsAuthenticated)
                throw new UnauthorizedException("You must be logged in");

            // Check permission (same user or admin)
            if (_currentUserService.UserId != request.UserId && !_currentUserService.IsAdmin)
                throw new ForbiddenException("You can only logout yourself");

            if (request.LogoutAllDevices)
            {
                // Clear all refresh tokens for this user
                await _unitOfWork.Auth.ClearRefreshTokenAsync(request.UserId);
            }
            else if (!string.IsNullOrEmpty(request.RefreshToken))
            {
                // Clear specific refresh token
                var appUser = await _unitOfWork.Auth.GetIdentityUserByRefreshTokenAsync(request.RefreshToken);
                if (appUser != null && appUser.Id == request.UserId)
                {
                    await _unitOfWork.Auth.ClearRefreshTokenAsync(request.UserId);
                }
            }

            await _unitOfWork.CompleteAsync(cancellationToken);
            return true;
        }
    }
}