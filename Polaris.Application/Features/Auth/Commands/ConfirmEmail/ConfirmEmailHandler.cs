using MediatR;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Interfaces.IRepositories;

namespace Polaris.Application.Features.Auth.Commands.ConfirmEmail
{
    public class ConfirmEmailHandler : IRequestHandler<ConfirmEmailCommand, ConfirmEmailResult>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ConfirmEmailHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ConfirmEmailResult> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        {
            // Check if user exists in Identity
            var user = await _unitOfWork.Auth.GetIdentityUserByIdAsync(request.UserId);
            if (user == null)
            {
                return new ConfirmEmailResult
                {
                    Succeeded = false,
                    Message = "User not found"
                };
            }

            // Check if already confirmed (using Identity)
            if (user.EmailConfirmed)
            {
                return new ConfirmEmailResult
                {
                    Succeeded = true,
                    Message = "Email already confirmed"
                };
            }

            // Confirm email using Identity (AuthRepository)
            var confirmed = await _unitOfWork.Auth.ConfirmEmailAsync(request.UserId, request.Token);

            if (!confirmed)
            {
                return new ConfirmEmailResult
                {
                    Succeeded = false,
                    Message = "Invalid or expired token"
                };
            }

            return new ConfirmEmailResult
            {
                Succeeded = true,
                Message = "Email confirmed successfully"
            };
        }
    }
}