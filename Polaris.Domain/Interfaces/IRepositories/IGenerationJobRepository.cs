using Polaris.Domain.Entities;

namespace Polaris.Domain.Interfaces.IRepositories
{
    /// <summary>
    /// Repository interface for GenerationJob entities.
    /// Defines the data access contract for generation jobs,
    /// following the Repository pattern of Clean Architecture.
    /// </summary>
    public interface IGenerationJobRepository
    {
        /// <summary>Creates a new generation job.</summary>
        Task<GenerationJob> CreateAsync(GenerationJob job, CancellationToken cancellationToken = default);

        /// <summary>Retrieves a generation job by its unique ID.</summary>
        Task<GenerationJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Updates an existing generation job.</summary>
        Task UpdateAsync(GenerationJob job, CancellationToken cancellationToken = default);

        /// <summary>Retrieves all jobs for a user, ordered by newest first.</summary>
        Task<List<GenerationJob>> GetUserJobsAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Retrieves a job by its associated Hangfire job ID.</summary>
        Task<GenerationJob?> GetByHangfireJobIdAsync(string hangfireJobId, CancellationToken cancellationToken = default);

        /// <summary>Retrieves the most recent active (Processing/Pending) job for a user (used by generation).</summary>
        Task<GenerationJob?> GetActiveJobByUserIdAsync(Guid userId);

        /// <summary>Retrieves all active jobs for a user, returning list of jobs.</summary>
        Task<List<GenerationJob>> GetActiveJobsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
