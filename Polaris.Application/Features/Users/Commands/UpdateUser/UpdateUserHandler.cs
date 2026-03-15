using AutoMapper;
using MediatR;
using Polaris.Application.Common.Exceptions;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Interfaces.IRepositories;
using System.ComponentModel.DataAnnotations;
using ValidationException = Polaris.Application.Common.Exceptions.ValidationException;

namespace Polaris.Application.Features.Users.Commands.UpdateUser
{
    public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, UpdateUserResult>
    {
        private const string ProfileImagesFolder = "profiles";
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public UpdateUserHandler(
            IUnitOfWork unitOfWork,
            IFileService fileService,
            IMapper mapper,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<UpdateUserResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            // Authentication check
            if (!_currentUserService.IsAuthenticated)
            {
                throw new UnauthorizedException("You must be logged in to update profile");
            }

            // Authorization check: users can only update their own profile unless they are admins
            if (_currentUserService.UserId != request.Id && !_currentUserService.IsAdmin)
            {
                throw new ForbiddenException("You don't have permission to update this user");
            }

            // Fetch the user to update 
            var user = await _unitOfWork.Users.GetByIdAsync(request.Id);
            if (user == null)
            {
                throw new NotFoundException($"User with ID {request.Id} not found");
            }

            // If email or username is changing, check for uniqueness
            if (user.Email != request.Email || user.UserName != request.UserName)
            {
                var (isUnique, takenFields) = await _unitOfWork.Users.CheckUserValidityAsync(
                    request.UserName, request.Email);

                if (!isUnique)
                {
                    var errors = string.Join(", ", takenFields.Select(f => $"{f} is already taken"));
                    throw new ValidationException(errors);
                }
            }

            user.FullName = request.FullName;
            user.UserName = request.UserName;
            user.Email = request.Email;

            if (request.ProfileImage != null)
            {
                // If there's an existing image, delete it before uploading the new one
                if (!string.IsNullOrEmpty(user.ImageUrl))
                {
                    await _fileService.DeleteFileAsync(user.ImageUrl, ProfileImagesFolder, cancellationToken);
                }

                // Upload the new profile image
                await using var stream = request.ProfileImage.OpenReadStream();
                user.ImageUrl = await _fileService.UploadFileAsync(
                    stream,
                    request.ProfileImage.FileName,
                    ProfileImagesFolder,
                    cancellationToken
                );
            }
            // If requested to remove the image and there's an existing one, delete it
            else if (request.RemoveImage && !string.IsNullOrEmpty(user.ImageUrl))
            {
                await _fileService.DeleteFileAsync(user.ImageUrl, ProfileImagesFolder, cancellationToken);
                user.ImageUrl = null;
            }

            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.CompleteAsync(cancellationToken);

            return _mapper.Map<UpdateUserResult>(user);
        }
    }
}