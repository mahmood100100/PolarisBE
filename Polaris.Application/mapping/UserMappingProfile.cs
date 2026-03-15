using AutoMapper;
using Polaris.Application.Features.Users.Commands.RegisterUser;
using Polaris.Application.Features.Users.Queries.GetUserById;
using Polaris.Domain.Entities;

namespace Polaris.Application.mapping
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<LocalUser, RegisterUserResult>();

            CreateMap<LocalUser, UserDetailsDto>()
                .ForMember(dest => dest.Roles, opt => opt.Ignore())
                .ForMember(dest => dest.ProjectsCount, opt => opt.Ignore())
                .ForMember(dest => dest.ConversationsCount, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
        }
    }
}
