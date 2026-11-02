using ecommercesite.Data;
using ecommercesite.Models;
using Microsoft.EntityFrameworkCore;

namespace ecommercesite.Repositories
{
    public class ProductCatRepository(ApplicationDbContext context) : GenericRepository<ProductCategory>(context), IProductCatRepository
    {
        public async Task<ProductCategory?> GetCategoryByIdAsync(int categoryId)
        {
            return await _context.ProductCategories
                                 .AsNoTracking()
                                 .Include(c => c.Products)
                                 .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        }

        public async Task<ProductCategory?> GetCategoryByNameAsync(string categoryName)
        {
            return await _context.ProductCategories
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(c => c.CategoryName == categoryName);
        }

        public async Task<bool> IsCategoryNameUniqueAsync(string categoryName)
        {
            return !await _context.ProductCategories
                                  .AnyAsync(c => c.CategoryName == categoryName);
        }
    }
}