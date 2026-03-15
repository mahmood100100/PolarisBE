using AutoMapper;
using Polaris.Application.Common.Exceptions;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Interfaces.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Application.Features.Users.Queries.GetUserById
{
    public class GetUserByIdHandler
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public GetUserByIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUserService currentUserService)
        {
            this._unitOfWork = unitOfWork;
            this._mapper = mapper;
            this._currentUserService = currentUserService;
        }

        public async Task<UserDetailsDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            if (!_currentUserService.IsAuthenticated)
                throw new UnauthorizedException("You must be logged in");

            if (_currentUserService.UserId != request.Id && !_currentUserService.IsAdmin)
                throw new ForbiddenException("No permission");

            var user = await _unitOfWork.Users.GetUserWithDetailsAsync(request.Id);
            if (user == null)
                throw new NotFoundException($"User {request.Id} not found");

            var roles = await _unitOfWork.Auth.GetUserRolesAsync(request.Id);

            var userDto = _mapper.Map<UserDetailsDto>(user);
            userDto.Roles = roles.ToList();

            return userDto;
        }
    }
}
