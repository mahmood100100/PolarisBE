using AutoMapper;
using Polaris.Application.Features.Users.Commands.RegisterUser;
using Polaris.Application.Features.Users.Commands.UpdateUser;
using Polaris.Application.Features.Users.Queries.GetAllUsers;
using Polaris.Application.Features.Users.Queries.GetUserById;
using Polaris.WebAPI.Common.Adapters;
using Polaris.WebAPI.Models.User;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        //Register mappings
        CreateMap<RegisterUserRequest, RegisterUserCommand>()
            .ForMember(dest => dest.ProfileImage,
                opt => opt.MapFrom(src =>
                    src.ProfileImage != null
                        ? new FormFileAdapter(src.ProfileImage)
                        : null));

        CreateMap<RegisterUserResult, UserResponse>()
            .ForMember(dest => dest.CreatedAt,
                opt => opt.MapFrom(src => src.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));

        // Update mappings
        CreateMap<UpdateUserRequest, UpdateUserCommand>()
            .ForMember(dest => dest.ProfileImage,
                opt => opt.MapFrom(src =>
                    src.ProfileImage != null
                        ? new FormFileAdapter(src.ProfileImage)
                        : null));

        // Use existing UserResponse for Update result
        CreateMap<UpdateUserResult, UserResponse>()
            .ForMember(dest => dest.CreatedAt,
                opt => opt.MapFrom(src => src.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")))
            .ForMember(dest => dest.UpdatedAt,
                opt => opt.MapFrom(src => src.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")));

        // GetAllUsers mapping
        CreateMap<UserListItemDto, UserListItemResponse>()
            .ForMember(dest => dest.CreatedAt,
                opt => opt.MapFrom(src => src.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));

        // GetUserById mapping
        CreateMap<UserDetailsDto, UserDetailsResponse>()
            .ForMember(dest => dest.CreatedAt,
                opt => opt.MapFrom(src => src.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")))
            .ForMember(dest => dest.UpdatedAt,
                opt => opt.MapFrom(src => src.UpdatedAt.HasValue
                    ? src.UpdatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss")
                    : null));

    }
}