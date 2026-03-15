using AutoMapper;
using Polaris.Application.Features.Auth.Commands.ChangePassword;
using Polaris.Application.Features.Auth.Commands.Login;
using Polaris.Application.Features.Auth.Commands.ResendConfirmationEmail;
using Polaris.Application.Features.Auth.Commands.ResetPassword;
using Polaris.Application.Features.Auth.Commands.SocialLogin;
using Polaris.WebAPI.Models.Auth;

namespace Polaris.WebAPI.mapping
{
    public class AuthMappingProfile : Profile
    {
        public AuthMappingProfile()
        {
            CreateMap<LoginRequest, LoginCommand>();
            CreateMap<ResendConfirmationRequest, ResendConfirmationCommand>();
            CreateMap<ResetPasswordRequest, ResetPasswordCommand>();
            CreateMap<ChangePasswordRequest, ChangePasswordCommand>();
            CreateMap<SocialLoginRequest, SocialLoginCommand>();
        }
    }
}
