using ecommercesite.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ecommercesite.Repositories
{
    /// <summary>
    /// Generic EF Core repository. Pushes filtering, ordering and paging
    /// down to SQL via <see cref="IQueryable{T}"/> so we don't materialize
    /// the whole table.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        // -------------------------------------------------------------------
        // Reads
        // -------------------------------------------------------------------
        public async Task<IReadOnlyList<T>> GetAllAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int? skip = null,
            int? take = null,
            bool asNoTracking = true)
        {
            IQueryable<T> query = _dbSet;

            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            if (filter is not null)
            {
                query = query.Where(filter);
            }

            if (orderBy is not null)
            {
                query = orderBy(query);
            }

            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }

            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
        {
            return filter is null
                ? await _dbSet.CountAsync()
                : await _dbSet.CountAsync(filter);
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        // -------------------------------------------------------------------
        // Writes
        // -------------------------------------------------------------------
        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}