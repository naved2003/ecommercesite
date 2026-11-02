using ecommercesite.Filters;
using ecommercesite.Models;
using ecommercesite.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ecommercesite.Controllers
{
    /// <summary>
    /// Product browsing + CRUD. Index and Details are open to any logged-in
    /// user (customers can browse the catalog). Create / Edit / Delete are
    /// admin-only via <c>[RequireAdmin]</c> on each action.
    /// </summary>
    [RequireLogin]
    public class ProductController(
        IProductService productService,
        IProductCatService productCatService,
        ILogger<ProductController> logger) : Controller
    {
        private readonly IProductService _productService = productService;
        private readonly IProductCatService _productCatService = productCatService;
        private readonly ILogger<ProductController> _logger = logger;

        // GET: /Product?search=&page=1
        [HttpGet]
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            const int pageSize = 5;

            var result = await _productService.GetProductsAsync(search, page, pageSize);

            ViewBag.CurrentPage = result.Page;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.Search = search;

            return View(result.Items);
        }

        // GET: /Product/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product is null)
            {
                return NotFound();
            }
            return View(product);
        }

        // GET: /Product/Create
        [HttpGet]
        [RequireAdmin]
        public async Task<IActionResult> Create()
        {
            await PopulateCategoriesViewBag();
            return View(new Product());
        }

        // POST: /Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAdmin]
        public async Task<IActionResult> Create(Product product)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCategoriesViewBag();
                return View(product);
            }

            var (success, error) = await _productService.AddProductAsync(product);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Create failed.");
                await PopulateCategoriesViewBag();
                return View(product);
            }

            TempData["Success"] = $"Product '{product.ProductName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Product/Edit/5
        [HttpGet]
        [RequireAdmin]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product is null)
            {
                return NotFound();
            }

            await PopulateCategoriesViewBag(product.CategoryId);
            return View(product);
        }

        // POST: /Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAdmin]
        public async Task<IActionResult> Edit(Product product)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCategoriesViewBag(product.CategoryId);
                return View(product);
            }

            var (success, error) = await _productService.UpdateProductAsync(product);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Update failed.");
                await PopulateCategoriesViewBag(product.CategoryId);
                return View(product);
            }

            TempData["Success"] = $"Product '{product.ProductName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Product/Delete/5
        [HttpGet]
        [RequireAdmin]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product is null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: /Product/DeleteConfirmed/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        [RequireAdmin]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deleted = await _productService.DeleteProductAsync(id);
            if (deleted)
            {
                TempData["Success"] = "Product deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Product not found.";
            }
            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error", new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------
        private async Task PopulateCategoriesViewBag(int? selected = null)
        {
            var categories = await _productCatService.GetAllProductcatAsync();
            ViewBag.Categories = categories
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName,
                    Selected = selected.HasValue && c.CategoryId == selected.Value
                })
                .OrderBy(c => c.Text)
                .ToList();
        }
    }
}