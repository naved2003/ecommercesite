using ecommercesite.Models;
using ecommercesite.Repositories;
using ecommercesite.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ecommercesite.Services
{
    /// <summary>
    /// Business logic for the <see cref="Product"/> aggregate. Validates
    /// that the chosen <see cref="ProductCategory"/> exists before insert /
    /// update, and that product names are unique.
    /// </summary>
    public class ProductService(
        IProductRepository productRepository,
        IProductCatRepository productCatRepository,
        ILogger<ProductService> logger) : IProductService
    {
        private readonly IProductRepository _productRepository = productRepository;
        private readonly IProductCatRepository _productCatRepository = productCatRepository;
        private readonly ILogger<ProductService> _logger = logger;

        public async Task<PagedResult<Product>> GetProductsAsync(string? search, int page, int pageSize)
        {
            return await _productRepository.GetProductsAsync(search, page, pageSize);
        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _productRepository.GetProductByIdAsync(productId);
        }

        public async Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _productRepository.GetProductsByCategoryAsync(categoryId);
        }

        public async Task<(bool Success, string? Error)> AddProductAsync(Product product)
        {
            if (!await _productCatRepository.ExistsAsync(c => c.CategoryId == product.CategoryId))
            {
                return (false, "Selected category does not exist.");
            }

            if (!await _productRepository.IsProductNameUniqueAsync(product.ProductName))
            {
                return (false, "A product with this name already exists.");
            }

            await _productRepository.AddAsync(product);
            await _productRepository.SaveAsync();

            _logger.LogInformation("Created product {Name} (Id={Id}, Category={CatId})",
                product.ProductName, product.ProductId, product.CategoryId);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateProductAsync(Product product)
        {
            var existing = await _productRepository.GetProductByIdAsync(product.ProductId);
            if (existing is null)
            {
                return (false, "Product not found.");
            }

            if (!await _productCatRepository.ExistsAsync(c => c.CategoryId == product.CategoryId))
            {
                return (false, "Selected category does not exist.");
            }

            // If the name has changed, enforce uniqueness against other rows.
            if (!string.Equals(existing.ProductName, product.ProductName, StringComparison.OrdinalIgnoreCase)
                && !await _productRepository.IsProductNameUniqueAsync(product.ProductName))
            {
                return (false, "A product with this name already exists.");
            }

            _productRepository.Update(product);
            await _productRepository.SaveAsync();

            _logger.LogInformation("Updated product {Id}", product.ProductId);
            return (true, null);
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            var stub = new Product { ProductId = productId };
            _productRepository.Delete(stub);
            await _productRepository.SaveAsync();

            _logger.LogInformation("Deleted product {Id}", productId);
            return true;
        }
    }
}
