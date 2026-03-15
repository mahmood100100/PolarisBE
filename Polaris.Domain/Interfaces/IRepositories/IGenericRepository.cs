using System.Linq.Expressions;

namespace Polaris.Domain.Interfaces.IRepositories
{
    public interface IGenericRepository<T> where T : class
    {
        // retrieves a paginated list of entities of type T from the database, with optional filtering and eager loading of related entities.
        Task<IEnumerable<T>> GetAllAsync(
            int pageSize = 10,
            int pageNumber = 1,
            string includeProperties = null,
            Expression<Func<T, bool>> filter = null
        );
        // retrieves a single entity of type T that matches the specified filter criteria, with optional eager loading of related entities and tracking behavior.
        Task<T> GetFirstOrDefaultAsync(
            Expression<Func<T, bool>> filter,
            string includeProperties = null,
            bool tracked = true
        );

        Task<T> GetByIdAsync(Guid id, string includeProperties = null);
        Task AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
    }
}
