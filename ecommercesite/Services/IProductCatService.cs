using ecommercesite.Models;
using ecommercesite.ViewModels;

namespace ecommercesite.Services
{
    public interface IProductCatService
    {
        Task<PagedResult<ProductCategory>> GetProductCategoriesAsync(string? search, int page, int pageSize);
        Task<IEnumerable<ProductCategory>> GetAllProductcatAsync();
        Task<ProductCategory?> GetProductcatByIdAsync(int categoryId);
        Task<ProductCategory?> GetProductcatByNameAsync(string categoryName);
        Task<(bool Success, string? Error)> AddProductCatAsync(ProductCategory prodcat);
        Task<(bool Success, string? Error)> UpdateProductCatAsync(ProductCategory prodcat);
        Task<bool> DeleteProductcatAsync(int categoryId);
    }
}