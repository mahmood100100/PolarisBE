using AutoMapper;
using MediatR;
using Polaris.Application.Common.Exceptions;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Entities;
using Polaris.Domain.Interfaces.IRepositories;
using System.Linq.Expressions;

namespace Polaris.Application.Features.Users.Queries.GetAllUsers
{
    public class GetAllUsersHandler : IRequestHandler<GetAllUsersQuery, List<UserListItemDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public GetAllUsersHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<List<UserListItemDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            // Check if user is authenticated
            if (!_currentUserService.IsAuthenticated)
            {
                throw new UnauthorizedException("You must be logged in to view users");
            }

            // Check if user is admin
            if (!_currentUserService.IsAdmin)
            {
                throw new ForbiddenException("Only admins can view all users");
            }

            // Get users with pagination and search
            var users = await _unitOfWork.GetGenericRepository<LocalUser>()
                .GetAllAsync(
                    pageSize: request.PageSize,
                    pageNumber: request.PageNumber,
                    filter: !string.IsNullOrEmpty(request.SearchTerm)
                        ? (Expression<Func<LocalUser, bool>>)(u =>
                            u.FullName.Contains(request.SearchTerm) ||
                            u.Email.Contains(request.SearchTerm) ||
                            u.UserName.Contains(request.SearchTerm))
                        : null
                );

            // Map to DTOs
            var userDtos = _mapper.Map<List<UserListItemDto>>(users);

            // Get roles for each user from AuthRepository
            foreach (var userDto in userDtos)
            {
                var roles = await _unitOfWork.Auth.GetUserRolesAsync(userDto.Id);
                userDto.Roles = roles.ToList();
            }

            return userDtos;
        }
    }
}