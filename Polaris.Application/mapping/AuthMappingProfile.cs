using AutoMapper;
using Polaris.Application.Features.Auth.Commands.Login;
using Polaris.Domain.Entities;

namespace Polaris.Application.mapping
{
    public class AuthMappingProfile : Profile
    {
        public AuthMappingProfile()
        {
             CreateMap<LocalUser, UserDto>()
                .ForMember(dest => dest.Roles, opt => opt.Ignore());
        }
    }
}
