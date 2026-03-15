using Polaris.Domain.Common.Models;
using Polaris.Domain.Entities;

namespace Polaris.Domain.Interfaces.IRepositories
{
    public interface IUserRepository : IGenericRepository<LocalUser>
    {
        // User validation
        Task<(bool IsUnique, string[] TakenFields)> CheckUserValidityAsync(string? username = null, string? email = null, Guid? excludeUserId = null);

        // User details
        Task<LocalUser?> GetUserWithDetailsAsync(Guid id);
        Task<LocalUser?> GetByEmailAsync(string email);
        Task<LocalUser?> GetByUserNameAsync(string userName);

        // User profile operations
        Task<OperationResult> CreateUserWithProfileAsync(LocalUser localUser);

        // Soft delete
        Task SoftDeleteAsync(Guid userId);
        Task RestoreUserAsync(Guid userId);
        Task<bool> UserExistsAsync(Guid userId);
    }
}