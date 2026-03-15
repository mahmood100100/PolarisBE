using AutoMapper;
using MediatR;
using Polaris.Application.Common.Exceptions;
using Polaris.Application.Common.Interfaces;
using Polaris.Application.Features.Auth.Commands.Login;
using Polaris.Application.Features.Users.Queries.GetUserById;
using Polaris.Domain.Entities;
using Polaris.Domain.Interfaces.IRepositories;

namespace Polaris.Application.Features.Users.Queries.GetCurrentUser
{
    public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDetailsDto?>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetCurrentUserQueryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            this._mapper = mapper;
        }

        public async Task<UserDetailsDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
            {
                throw new UnauthorizedException("User not authenticated");
            }

            var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);

            if (user == null)
            {
                throw new NotFoundException($"User with ID {userId} not found");
            }

            UserDetailsDto userDto = _mapper.Map<UserDetailsDto>(user);

            return userDto;
        }
    }
}