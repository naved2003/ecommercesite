using ecommercesite.Models;
using ecommercesite.Repositories;
using ecommercesite.ViewModels;
using System.Linq.Expressions;

namespace ecommercesite.Services
{
    public class ProductCatService(
        IProductCatRepository productCatRepository,
        ILogger<ProductCatService> logger) : IProductCatService
    {
        private readonly IProductCatRepository _productCatRepository = productCatRepository;
        private readonly ILogger<ProductCatService> _logger = logger;

        public async Task<PagedResult<ProductCategory>> GetProductCategoriesAsync(string? search, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

            var term = search?.Trim();
            Expression<Func<ProductCategory, bool>>? filter = null;
            if (!string.IsNullOrEmpty(term))
            {
                filter = c => c.CategoryName.Contains(term);
            }

            var total = await _productCatRepository.CountAsync(filter);
            var items = await _productCatRepository.GetAllAsync(
                filter: filter,
                orderBy: q => q.OrderBy(c => c.CategoryName),
                skip: (page - 1) * pageSize,
                take: pageSize,
                asNoTracking: true);

            return new PagedResult<ProductCategory>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<ProductCategory>> GetAllProductcatAsync()
        {
            return await _productCatRepository.GetAllAsync(
                orderBy: q => q.OrderBy(c => c.CategoryName));
        }

        public async Task<ProductCategory?> GetProductcatByIdAsync(int categoryId)
        {
            return await _productCatRepository.GetCategoryByIdAsync(categoryId);
        }

        public async Task<ProductCategory?> GetProductcatByNameAsync(string categoryName)
        {
            return await _productCatRepository.GetCategoryByNameAsync(categoryName);
        }

        public async Task<(bool Success, string? Error)> AddProductCatAsync(ProductCategory prodcat)
        {
            if (!await _productCatRepository.IsCategoryNameUniqueAsync(prodcat.CategoryName))
            {
                return (false, "A category with this name already exists.");
            }

            await _productCatRepository.AddAsync(prodcat);
            await _productCatRepository.SaveAsync();

            _logger.LogInformation("Created product category {Name} ({Id})",
                prodcat.CategoryName, prodcat.CategoryId);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateProductCatAsync(ProductCategory prodcat)
        {
            // Fetch the existing row as a TRACKED entity so we can mutate
            // only the editable fields instead of calling Update(stub),
            // which would mark every column Modified and could NULL out
            // server-managed fields (CreatedAt, etc.).
            var existing = await _productCatRepository.GetCategoryByIdAsync(prodcat.CategoryId);
            if (existing is null)
            {
                return (false, "Category not found.");
            }

            // Enforce name uniqueness on rename (case-sensitive comparison
            // against the existing name so we don't reject a no-op rename).
            if (!string.Equals(existing.CategoryName, prodcat.CategoryName, StringComparison.Ordinal)
                && !await _productCatRepository.IsCategoryNameUniqueAsync(prodcat.CategoryName))
            {
                return (false, "A category with this name already exists.");
            }

            existing.CategoryName = prodcat.CategoryName;
            existing.Description = prodcat.Description;

            await _productCatRepository.SaveAsync();

            _logger.LogInformation("Updated product category {Id}", prodcat.CategoryId);
            return (true, null);
        }

        public async Task<bool> DeleteProductcatAsync(int categoryId)
        {
            var stub = new ProductCategory { CategoryId = categoryId };
            _productCatRepository.Delete(stub);
            await _productCatRepository.SaveAsync();

            _logger.LogInformation("Deleted product category {Id}", categoryId);
            return true;
        }
    }
}
