using Microsoft.EntityFrameworkCore;
using Polaris.Domain.Common.Models;
using Polaris.Domain.Entities;
using Polaris.Domain.Interfaces.IRepositories;
using Polaris.Infrastructure.Data;

namespace Polaris.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<LocalUser>, IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        // Check user validity (username/email uniqueness)
        public async Task<(bool IsUnique, string[] TakenFields)> CheckUserValidityAsync(
            string? username = null,
            string? email = null,
            Guid? excludeUserId = null)
        {
            var takenFields = new List<string>();
            var query = _context.Set<LocalUser>().AsQueryable();

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            if (!string.IsNullOrEmpty(username) && await query.AnyAsync(u => u.UserName == username))
                takenFields.Add("Username");

            if (!string.IsNullOrEmpty(email) && await query.AnyAsync(u => u.Email == email))
                takenFields.Add("Email");

            return (takenFields.Count == 0, takenFields.ToArray());
        }

        // Get user by email
        public async Task<LocalUser?> GetByEmailAsync(string email)
        {
            return await _context.Set<LocalUser>()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        // Get user by username
        public async Task<LocalUser?> GetByUserNameAsync(string userName)
        {
            return await _context.Set<LocalUser>()
                .FirstOrDefaultAsync(u => u.UserName == userName);
        }

        // Get user with details (Projects, Conversations)
        public async Task<LocalUser?> GetUserWithDetailsAsync(Guid id)
        {
            return await _context.Set<LocalUser>()
                .Include(u => u.Projects)
                .Include(u => u.Conversations)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        // Check if user exists
        public async Task<bool> UserExistsAsync(Guid userId)
        {
            return await _context.Set<LocalUser>().AnyAsync(u => u.Id == userId);
        }

        // Soft delete
        public async Task SoftDeleteAsync(Guid userId)
        {
            var user = await GetByIdAsync(userId);
            if (user != null)
            {
                // Add IsDeleted property to LocalUser if needed
                // user.IsDeleted = true;
                // user.DeletedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        // Restore soft-deleted user
        public async Task RestoreUserAsync(Guid userId)
        {
            var user = await GetByIdAsync(userId);
            if (user != null)
            {
                // user.IsDeleted = false;
                // user.DeletedAt = null;
                Update(user);
            }
        }

        // Create user with profile
        public async Task<OperationResult> CreateUserWithProfileAsync(LocalUser localUser)
        {
            try
            {
                if (localUser.Id == Guid.Empty)
                    localUser.Id = Guid.NewGuid();

                localUser.CreatedAt = DateTime.UtcNow;

                await _context.Set<LocalUser>().AddAsync(localUser);
                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Failed to create user profile: {ex.Message}");
            }
        }
    }
}