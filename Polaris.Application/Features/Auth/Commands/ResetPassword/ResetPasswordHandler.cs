using MediatR;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Interfaces.IRepositories;

namespace Polaris.Application.Features.Auth.Commands.ResetPassword
{
    public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResult>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ResetPasswordHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResetPasswordResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            // Check if passwords match
            if (request.NewPassword != request.ConfirmPassword)
            {
                return new ResetPasswordResult
                {
                    Succeeded = false,
                    Message = "Passwords do not match"
                };
            }

            // Find user by email
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return new ResetPasswordResult
                {
                    Succeeded = false,
                    Message = "Invalid request"
                };
            }

            // Validate numeric token first
            var isValidToken = await _unitOfWork.Auth.ValidateResetTokenAsync(user.Email, request.Token);
            if (!isValidToken)
            {
                return new ResetPasswordResult
                {
                    Succeeded = false,
                    Message = "Invalid or expired token"
                };
            }

            // Generate fresh identity token from UserManager
            var identityToken = await _unitOfWork.Auth.GeneratePasswordResetTokenAsync(user.Email);

            // Reset password using the identity token
            var result = await _unitOfWork.Auth.ResetPasswordAsync(user.Email, identityToken, request.NewPassword);

            if (!result)
            {
                return new ResetPasswordResult
                {
                    Succeeded = false,
                    Message = "Failed to reset password"
                };
            }

            // Mark numeric token as used
            await _unitOfWork.Auth.MarkResetTokenAsUsedAsync(user.Email);

            return new ResetPasswordResult
            {
                Succeeded = true,
                Message = "Password reset successfully"
            };
        }
    }
}