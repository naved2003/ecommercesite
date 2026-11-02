using ecommercesite.Data;
using ecommercesite.Models;
using ecommercesite.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ecommercesite.Repositories
{
    public class ProductRepository(ApplicationDbContext context) : GenericRepository<Product>(context), IProductRepository
    {
        public async Task<PagedResult<Product>> GetProductsAsync(string? search, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

            var term = search?.Trim();
            IQueryable<Product> query = _context.Products
                                                .AsNoTracking()
                                                .Include(p => p.ProductCategory);

            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(p =>
                    p.ProductName.Contains(term) ||
                    (p.ProductCategory != null && p.ProductCategory.CategoryName.Contains(term)));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.ProductName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Product>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _context.Products
                                 .AsNoTracking()
                                 .Include(p => p.ProductCategory)
                                 .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _context.Products
                                 .AsNoTracking()
                                 .Where(p => p.CategoryId == categoryId)
                                 .OrderBy(p => p.ProductName)
                                 .ToListAsync();
        }

        public async Task<bool> IsProductNameUniqueAsync(string productName)
        {
            return !await _context.Products.AnyAsync(p => p.ProductName == productName);
        }
    }
}