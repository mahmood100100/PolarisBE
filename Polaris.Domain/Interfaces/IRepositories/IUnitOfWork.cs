namespace Polaris.Domain.Interfaces.IRepositories
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        IAuthRepository Auth { get; }
        IGenericRepository<T> GetGenericRepository<T>() where T : class;

        Task<int> CompleteAsync(CancellationToken cancellationToken = default);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action);
        Task RollbackAsync();
    }
}
