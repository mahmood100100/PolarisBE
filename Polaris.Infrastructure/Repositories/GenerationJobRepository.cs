using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polaris.Domain.Entities;
using Polaris.Domain.Interfaces.IRepositories;
using Polaris.Infrastructure.Data;

namespace Polaris.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for GenerationJob entities.
    /// Provides CRUD operations and specialized queries for generation jobs
    /// using Entity Framework Core and PostgreSQL.
    ///
    ///   Design Note on SaveChanges:
    ///   Unlike other repositories, this implementation calls SaveChangesAsync
    ///   directly in <see cref="CreateAsync"/> and <see cref="UpdateAsync"/>.
    ///   This is intentional because <see cref="GenerationJobProcessor"/> runs
    ///   inside a Hangfire background worker with its own DI scope — outside the
    ///   normal UnitOfWork request lifecycle. Each incremental save during a
    ///   long streaming operation must be committed immediately for data durability.
    ///   All other usages (via UnitOfWork) should call UoW.CompleteAsync() instead.
    /// </summary>
    public class GenerationJobRepository : IGenerationJobRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<GenerationJobRepository> _logger;

        public GenerationJobRepository(AppDbContext context, ILogger<GenerationJobRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new generation job in the database.
        /// Calls SaveChangesAsync directly (see class-level design note).
        /// </summary>
        public async Task<GenerationJob> CreateAsync(
            GenerationJob job,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.GenerationJobs.AddAsync(job, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "Job created with ID: {JobId} for user: {UserId}", job.Id, job.UserId);
                return job;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job for user: {UserId}", job.UserId);
                throw;
            }
        }

        /// <summary>Retrieves a generation job by its unique identifier.</summary>
        public async Task<GenerationJob?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _context.GenerationJobs
                .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
        }

        /// <summary>
        /// Updates an existing generation job in the database.
        /// Calls SaveChangesAsync directly (see class-level design note).
        /// Used frequently during processing to save progress and partial results.
        /// </summary>
        public async Task UpdateAsync(
            GenerationJob job,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _context.GenerationJobs.Update(job);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogDebug(
                    "Job updated: {JobId} - Status: {Status}", job.Id, job.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job: {JobId}", job.Id);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all generation jobs for a specific user, ordered by creation date (newest first).
        /// </summary>
        public async Task<List<GenerationJob>> GetUserJobsAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.GenerationJobs
                .Where(j => j.UserId == userId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves a generation job by its Hangfire background job ID.
        /// Used for correlation between Hangfire and application-level job tracking.
        /// </summary>
        public async Task<GenerationJob?> GetByHangfireJobIdAsync(
            string hangfireJobId,
            CancellationToken cancellationToken = default)
        {
            return await _context.GenerationJobs
                .FirstOrDefaultAsync(j => j.HangfireJobId == hangfireJobId, cancellationToken);
        }

        /// <summary>
        /// Retrieves the most recent active (Processing or Pending) job for a user.
        /// Returns null if the user has no active jobs.
        /// </summary>
        public async Task<GenerationJob?> GetActiveJobByUserIdAsync(Guid userId)
        {
            return await _context.GenerationJobs
                .Where(j => j.UserId == userId &&
                           (j.Status == "Processing" || j.Status == "Pending"))
                .OrderByDescending(j => j.CreatedAt)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Retrieves all active (Processing or Pending) jobs for a user.
        /// Useful for reconnecting multiple chat streams when refreshing the page.
        /// </summary>
        public async Task<List<GenerationJob>> GetActiveJobsByUserIdAsync(
            Guid userId, 
            CancellationToken cancellationToken = default)
        {
            return await _context.GenerationJobs
                .Where(j => j.UserId == userId &&
                           (j.Status == "Processing" || j.Status == "Pending"))
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}