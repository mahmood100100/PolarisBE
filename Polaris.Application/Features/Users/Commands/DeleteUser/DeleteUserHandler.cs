using MediatR;
using Polaris.Application.Common.Exceptions;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Interfaces.IRepositories;

namespace Polaris.Application.Features.Users.Commands.DeleteUser
{
    public class DeleteUserHandler : IRequestHandler<DeleteUserCommand, bool>
    {
        private const string ProfileImagesFolder = "profiles";
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;
        private readonly ICurrentUserService _currentUserService;

        public DeleteUserHandler(
            IUnitOfWork unitOfWork,
            IFileService fileService,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            // Check if user is authenticated
            if (!_currentUserService.IsAuthenticated)
                throw new UnauthorizedException("You must be logged in");

            // Check permission (same user or admin)
            if (_currentUserService.UserId != request.Id && !_currentUserService.IsAdmin)
                throw new ForbiddenException("You don't have permission to delete this user");

            // Get user from database
            var localUser = await _unitOfWork.Users.GetByIdAsync(request.Id);
            if (localUser == null)
                throw new NotFoundException($"User with ID {request.Id} not found");

            // Save image URL before deletion
            var imageUrl = localUser.ImageUrl;

            // Delete user in transaction
            var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Delete from LocalUser
                _unitOfWork.Users.Remove(localUser);

                // Delete from Identity
                await _unitOfWork.Auth.DeleteIdentityUserAsync(request.Id);

                var saveResult = await _unitOfWork.CompleteAsync(cancellationToken);
                return saveResult > 0;
            });

            // Delete profile image after successful database deletion
            if (result && !string.IsNullOrEmpty(imageUrl))
            {
                try
                {
                    await _fileService.DeleteFileAsync(imageUrl, ProfileImagesFolder, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the operation
                    Console.WriteLine($"Failed to delete image: {ex.Message}");
                }
            }

            return result;
        }
    }
}