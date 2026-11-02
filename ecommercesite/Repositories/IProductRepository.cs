using ecommercesite.Models;
using ecommercesite.ViewModels;

namespace ecommercesite.Repositories
{
    /// <summary>
    /// Product-specific queries on top of the generic CRUD surface.
    /// Product queries always Include the parent <see cref="ProductCategory"/>
    /// so the UI can show the category name without an N+1 problem.
    /// </summary>
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<PagedResult<Product>> GetProductsAsync(string? search, int page, int pageSize);
        Task<Product?> GetProductByIdAsync(int productId);
        Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<bool> IsProductNameUniqueAsync(string productName);
    }
}