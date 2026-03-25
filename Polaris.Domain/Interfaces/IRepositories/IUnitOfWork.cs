namespace Polaris.Domain.Interfaces.IRepositories
{
    /// <summary>
    /// Unit of Work pattern interface.
    /// Provides a single entry point for all repositories and coordinates
    /// database transactions to ensure atomicity across multiple operations.
    /// </summary>
    public interface IUnitOfWork
    {
        // ─── Specialized Repositories ────────────────────────────────────
        IUserRepository Users { get; }
        IAuthRepository Auth { get; }
        IGenerationJobRepository GenerationJobs { get; }
        IConversationRepository Conversations { get; }
        IMessageRepository Messages { get; }

        // ─── Generic Repository Factory ──────────────────────────────────
        IGenericRepository<T> GetGenericRepository<T>() where T : class;

        // ─── Transaction Management ──────────────────────────────────────
        Task<int> CompleteAsync(CancellationToken cancellationToken = default);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action);
        Task RollbackAsync();
    }
}
