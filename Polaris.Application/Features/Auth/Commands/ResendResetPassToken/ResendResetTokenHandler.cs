using MediatR;
using Polaris.Application.Common.Interfaces;
using Polaris.Application.Features.Auth.Commands.ResendResetPassToken;
using Polaris.Domain.Interfaces.IRepositories;

namespace Polaris.Application.Features.Auth.Commands.ResendResetToken
{
    public class ResendResetTokenHandler : IRequestHandler<ResendResetTokenCommand, ResendResetTokenResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        public ResendResetTokenHandler(
            IUnitOfWork unitOfWork,
            IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task<ResendResetTokenResult> Handle(ResendResetTokenCommand request, CancellationToken cancellationToken)
        {
            // Find user by email
            var user = await _unitOfWork.Auth.GetIdentityUserByEmailAsync(request.Email);

            // Always return success (security)
            if (user == null)
            {
                return new ResendResetTokenResult
                {
                    Succeeded = true,
                    Message = "If your email is registered, a reset code has been sent"
                };
            }

            if (user.ResetPasswordToken != null &&
                user.ResePasswordTokenUsed == false &&
                user.ResetPasswordTokenExpiresAt > DateTime.UtcNow)
            {
                var remainingMinutes = (int)Math.Ceiling((user.ResetPasswordTokenExpiresAt.Value - DateTime.UtcNow).TotalMinutes);
                var remainingSeconds = (int)Math.Ceiling((user.ResetPasswordTokenExpiresAt.Value - DateTime.UtcNow).TotalSeconds);

                string timeMessage;
                if (remainingMinutes > 1)
                {
                    timeMessage = $"{remainingMinutes} minutes";
                }
                else if (remainingMinutes == 1)
                {
                    timeMessage = "1 minute";
                }
                else
                {
                    timeMessage = $"{remainingSeconds} seconds";
                }

                return new ResendResetTokenResult
                {
                    Succeeded = false,
                    Message = $"A valid reset code was already sent to your email. This code will expire in {timeMessage}. Please check your email or wait for it to expire before requesting a new one."
                };
            }

            string tokenToSend = GenerateNumericToken();

            // Save new token
            await _unitOfWork.Auth.SaveResetTokenAsync(user.Email, tokenToSend);

            // Send email with new token
            await _emailService.SendPasswordResetEmailAsync(user.Email, tokenToSend);

            return new ResendResetTokenResult
            {
                Succeeded = true,
                Message = "If your email is registered, a reset code has been sent"
            };
        }

        private string GenerateNumericToken()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}