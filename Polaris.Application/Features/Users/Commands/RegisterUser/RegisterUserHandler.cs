using AutoMapper;
using MediatR;
using Polaris.Application.Common.Exceptions;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Entities;
using Polaris.Domain.Interfaces.IRepositories;

namespace Polaris.Application.Features.Users.Commands.RegisterUser
{
    public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, RegisterUserResult>
    {
        private const string ProfileImagesFolder = "profiles";
        private const string DefaultRole = "user";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly ILinkGeneratorService _linkGenerator;

        public RegisterUserHandler(
            IUnitOfWork unitOfWork,
            IFileService fileService,
            IMapper mapper,
            IEmailService emailService,
            ILinkGeneratorService linkGenerator)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
            _mapper = mapper;
            _emailService = emailService;
            _linkGenerator = linkGenerator;
        }

        public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            // Check if username or email is already taken (LocalUser)
            var (isUnique, takenFields) = await _unitOfWork.Users.CheckUserValidityAsync(
                request.UserName, request.Email);

            if (!isUnique)
            {
                var errors = takenFields.Select(f => $"{f} is already taken").ToArray();
                throw new ValidationException(string.Join(", ", errors));
            }

            // Check if identity user exists (Auth)
            var existingIdentity = await _unitOfWork.Auth.GetIdentityUserByEmailAsync(request.Email);
            if (existingIdentity != null)
            {
                throw new ValidationException("Email is already registered");
            }

            // Perform registration in a transaction
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                string? imageUrl = null;

                // Upload profile image if provided
                if (request.ProfileImage != null && request.ProfileImage.Length > 0)
                {
                    await using var stream = request.ProfileImage.OpenReadStream();
                    imageUrl = await _fileService.UploadFileAsync(
                        stream,
                        request.ProfileImage.FileName,
                        ProfileImagesFolder,
                        cancellationToken
                    );
                }

                // Create LocalUser
                var localUser = new LocalUser
                {
                    Id = Guid.NewGuid(), // Generate ID first
                    FullName = request.FullName,
                    UserName = request.UserName,
                    Email = request.Email,
                    ImageUrl = imageUrl ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                // Save LocalUser profile (without password)
                await _unitOfWork.Users.AddAsync(localUser);

                // Create Identity user with password (using AuthRepository)
                var authResult = await _unitOfWork.Auth.CreateIdentityUserAsync(localUser, request.Password);

                if (!authResult.Succeeded)
                {
                    var errors = authResult.Errors ?? new[] { "Failed to create identity user" };
                    throw new ValidationException(string.Join(", ", errors));
                }

                // Assign default role to the new user
                var roleResult = await _unitOfWork.Auth.AddToRoleAsync(localUser.Id, DefaultRole);

                if (!roleResult.Succeeded)
                {
                    var errors = roleResult.Errors ?? new[] { "Failed to assign role" };
                    throw new ValidationException(string.Join(", ", errors));
                }

                // Generate email confirmation token using AuthRepository
                var token = await _unitOfWork.Auth.GenerateEmailConfirmationTokenAsync(localUser.Id);

                // Generate confirmation link using LinkGenerator
                var confirmationLink = _linkGenerator.GenerateEmailConfirmationLink(localUser.Id, token);

                // Send email using EmailService
                await _emailService.SendEmailConfirmationAsync(localUser.Email, confirmationLink , localUser.FullName);

                // Map to result
                var userResult = _mapper.Map<RegisterUserResult>(localUser);

                return userResult;
            });
        }
    }
}