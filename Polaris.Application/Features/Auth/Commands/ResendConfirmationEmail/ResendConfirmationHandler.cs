using MediatR;
using Polaris.Application.Common.Interfaces;
using Polaris.Application.Features.Auth.Commands.ResendConfirmationEmail;
using Polaris.Domain.Interfaces.IRepositories;

namespace Polaris.Application.Features.Auth.Commands.ResendConfirmation
{
    public class ResendConfirmationHandler : IRequestHandler<ResendConfirmationCommand, ResendConfirmationResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILinkGeneratorService _linkGenerator;
        private readonly IEmailService _emailService;

        public ResendConfirmationHandler(
            IUnitOfWork unitOfWork,
            ILinkGeneratorService linkGenerator,
            IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _linkGenerator = linkGenerator;
            _emailService = emailService;
        }

        public async Task<ResendConfirmationResult> Handle(ResendConfirmationCommand request, CancellationToken cancellationToken)
        {
            // 1. Find user by email
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            if (user == null)
            {
                // Return success even if user not found (security)
                return new ResendConfirmationResult
                {
                    Succeeded = true,
                    Message = "If your email is registered, you will receive a confirmation link"
                };
            }

            // 2. Check if email is already confirmed
            var isConfirmed = await _unitOfWork.Auth.IsEmailConfirmedAsync(user.Id);
            if (isConfirmed)
            {
                return new ResendConfirmationResult
                {
                    Succeeded = false,
                    Message = "Email is already confirmed"
                };
            }

            // 3. Generate new confirmation token
            var token = await _unitOfWork.Auth.GenerateEmailConfirmationTokenAsync(user.Id);

            // 4. Generate confirmation link
            var confirmationLink = _linkGenerator.GenerateEmailConfirmationLink(user.Id, token);

            // 5. Send email with user's name
            await _emailService.SendEmailConfirmationAsync(
                user.Email,
                confirmationLink,
                user.FullName
            );

            return new ResendConfirmationResult
            {
                Succeeded = true,
                Message = "Confirmation email sent successfully"
            };
        }
    }
}