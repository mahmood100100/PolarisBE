using AutoMapper;
using MediatR;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Interfaces.IRepositories;

namespace Polaris.Application.Features.Auth.Commands.Login
{
    public class LoginHandler : IRequestHandler<LoginCommand, LoginResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly ILinkGeneratorService _linkGenerator;

        public LoginHandler(
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            IMapper mapper,
            IEmailService emailService,
            ILinkGeneratorService linkGenerator)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _mapper = mapper;
            _emailService = emailService;
            _linkGenerator = linkGenerator;
        }

        public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Find user by email using AuthRepository
                var appUser = await _unitOfWork.Auth.GetIdentityUserByEmailAsync(request.Email);
                if (appUser == null)
                {
                    return new LoginResult
                    {
                        Succeeded = false,
                        Errors = new[] { "Invalid email or password" }
                    };
                }

                // Check password using AuthRepository
                var passwordValid = await _unitOfWork.Auth.CheckPasswordAsync(appUser.Id, request.Password);
                if (!passwordValid)
                {
                    return new LoginResult
                    {
                        Succeeded = false,
                        Errors = new[] { "Invalid email or password" }
                    };
                }

                if (!appUser.EmailConfirmed)
                {
                    var isTokenValid = await _unitOfWork.Auth.IsEmailConfirmationTokenValidAsync(appUser.Email);

                    if (isTokenValid)
                    {
                        return new LoginResult
                        {
                            Succeeded = false,
                            RequiresEmailConfirmation = true,
                            EmailConfirmationStatus = "valid_token_exists",
                            Message = "Please confirm your email before logging in. A verification link was already sent to your email.",
                            Errors = new[] { "Email not confirmed. Check your inbox for the verification link." }
                        };
                    }
                    else
                    {
                        var newToken = await _unitOfWork.Auth.GenerateEmailConfirmationTokenAsync(appUser.Id);

                        var confirmationLink = _linkGenerator.GenerateEmailConfirmationLink(appUser.Id, newToken);

                        await _emailService.SendEmailConfirmationAsync(appUser.Email, confirmationLink, appUser.UserName);

                        return new LoginResult
                        {
                            Succeeded = false,
                            RequiresEmailConfirmation = true,
                            EmailConfirmationStatus = "new_token_sent",
                            Message = "Your email is not confirmed. A new verification link has been sent to your email.",
                            Errors = new[] { "Email not confirmed. A new verification link has been sent." }
                        };
                    }
                }

                // Get local user details using UserRepository
                var localUser = await _unitOfWork.Users.GetByIdAsync(appUser.Id);
                if (localUser == null)
                {
                    return new LoginResult
                    {
                        Succeeded = false,
                        Errors = new[] { "User profile not found" }
                    };
                }

                // Get user roles using AuthRepository
                var roles = await _unitOfWork.Auth.GetUserRolesAsync(appUser.Id);

                // Generate tokens using TokenService
                var accessToken = _tokenService.GenerateAccessToken(localUser, roles);
                var refreshToken = _tokenService.GenerateRefreshToken();
                var expiresAt = DateTime.UtcNow.AddMinutes(15);

                // Save refresh token using AuthRepository
                await _unitOfWork.Auth.UpdateRefreshTokenAsync(
                    appUser.Id,
                    refreshToken,
                    DateTime.UtcNow.AddDays(7));

                // Update last login date using UserRepository
                localUser.LastLoginAt = DateTime.UtcNow;
                await _unitOfWork.CompleteAsync(cancellationToken);

                // Prepare user DTO
                var userDto = _mapper.Map<UserDto>(localUser);
                userDto.Roles = roles.ToList();

                return new LoginResult
                {
                    Succeeded = true,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                return new LoginResult
                {
                    Succeeded = false,
                    Errors = new[] { $"Login failed: {ex.Message}" }
                };
            }
        }
    }
}