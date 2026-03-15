using MediatR;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Interfaces.IRepositories;

namespace Polaris.Application.Features.Auth.Commands.ForgotPassword
{
    public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        public ForgotPasswordHandler(
            IUnitOfWork unitOfWork,
            IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task<ForgotPasswordResult> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            // Find user by email
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);

            // Always return success (security - don't reveal if email exists)
            if (user == null)
            {
                return new ForgotPasswordResult
                {
                    Succeeded = true,
                    Message = "If your email is registered, you will receive a password reset code"
                };
            }

            // Generate numeric token only (6 digits)
            string numericToken = GenerateNumericToken();

            // Save numeric token directly to user entity (no need to store identity token)
            await _unitOfWork.Auth.SaveResetTokenAsync(user.Email, numericToken);

            // Send email with numeric token only
            await _emailService.SendPasswordResetEmailAsync(user.Email, numericToken);

            return new ForgotPasswordResult
            {
                Succeeded = true,
                Message = "If your email is registered, you will receive a password reset code"
            };
        }

        private string GenerateNumericToken()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}