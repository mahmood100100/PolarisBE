using MediatR;
using Polaris.Application.Common.Exceptions;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Interfaces.IRepositories;

namespace Polaris.Application.Features.Auth.Commands.ChangePassword
{
    public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, ChangePasswordResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public ChangePasswordHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<ChangePasswordResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            // Check if user is authenticated
            if (!_currentUserService.IsAuthenticated)
            {
                throw new UnauthorizedException("You must be logged in to change password");
            }

            // Check if user is changing their own password
            if (_currentUserService.UserId != request.UserId && !_currentUserService.IsAdmin)
            {
                throw new ForbiddenException("You can only change your own password");
            }

            // Check if passwords match
            if (request.NewPassword != request.ConfirmPassword)
            {
                return new ChangePasswordResult
                {
                    Succeeded = false,
                    Message = "New passwords do not match"
                };
            }

            // Change password using Identity
            var result = await _unitOfWork.Auth.ChangePasswordAsync(
                request.UserId,
                request.CurrentPassword,
                request.NewPassword
            );

            if (!result.Succeeded)
            {
                return new ChangePasswordResult
                {
                    Succeeded = false,
                    Message = result.Errors?.FirstOrDefault() ?? "Failed to change password"
                };
            }

            return new ChangePasswordResult
            {
                Succeeded = true,
                Message = "Password changed successfully"
            };
        }
    }
}