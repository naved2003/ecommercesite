using ecommercesite.Models;

namespace ecommercesite.Repositories
{
    /// <summary>
    /// Product-category repository. The legacy interface names
    /// (GetProductByIdAsync / GetProductByNameAsync) are kept as thin aliases
    /// so existing call sites keep compiling, but the new idiom is
    /// <c>GetCategoryByIdAsync</c> / <c>GetCategoryByNameAsync</c>.
    /// </summary>
    public interface IProductCatRepository : IGenericRepository<ProductCategory>
    {
        Task<ProductCategory?> GetCategoryByIdAsync(int categoryId);
        Task<ProductCategory?> GetCategoryByNameAsync(string categoryName);
        Task<bool> IsCategoryNameUniqueAsync(string categoryName);

        // Legacy aliases (kept for backward compatibility).
        Task<ProductCategory?> GetProductByIdAsync(int catId) => GetCategoryByIdAsync(catId);
        Task<ProductCategory?> GetProductByNameAsync(string catName) => GetCategoryByNameAsync(catName);
    }
}