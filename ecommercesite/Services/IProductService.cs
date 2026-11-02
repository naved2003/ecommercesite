using ecommercesite.Models;
using ecommercesite.ViewModels;

namespace ecommercesite.Services
{
    public interface IProductService
    {
        Task<PagedResult<Product>> GetProductsAsync(string? search, int page, int pageSize);
        Task<Product?> GetProductByIdAsync(int productId);
        Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<(bool Success, string? Error)> AddProductAsync(Product product);
        Task<(bool Success, string? Error)> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int productId);
    }
}