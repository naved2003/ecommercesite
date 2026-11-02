using System.Linq.Expressions;

namespace ecommercesite.Repositories
{
    /// <summary>
    /// Generic repository contract. Provides the small CRUD surface every
    /// aggregate root needs. Specialized repositories extend this to add
    /// entity-specific queries.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public interface IGenericRepository<T> where T : class
    {
        // --- Reads ----------------------------------------------------------
        Task<IReadOnlyList<T>> GetAllAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int? skip = null,
            int? take = null,
            bool asNoTracking = true);

        Task<T?> GetByIdAsync(object id);

        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);

        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        // --- Writes ---------------------------------------------------------
        Task AddAsync(T entity);

        void Update(T entity);

        void Delete(T entity);

        Task SaveAsync();
    }
}