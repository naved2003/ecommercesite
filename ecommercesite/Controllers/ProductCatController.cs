using ecommercesite.Filters;
using ecommercesite.Models;
using ecommercesite.Services;
using ecommercesite.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ecommercesite.Controllers
{
    /// <summary>
    /// Category CRUD — admin only. Customers can browse products but cannot
    /// create/edit/delete categories.
    /// </summary>
    [RequireLogin]
    [RequireAdmin]
    public class ProductCatController(
        ILogger<ProductCatController> logger,
        IProductCatService productCatService) : Controller
    {
        private readonly ILogger<ProductCatController> _logger = logger;
        private readonly IProductCatService _productCatService = productCatService;

        // GET: ProductCat/Index?search=&page=1
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            const int pageSize = 5;

            var result = await _productCatService.GetProductCategoriesAsync(search, page, pageSize);

            ViewBag.CurrentPage = result.Page;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.Search = search;

            return View(result.Items);
        }

        // GET: ProductCat/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ProductCat/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCategory model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, error) = await _productCatService.AddProductCatAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Create failed.");
                return View(model);
            }

            TempData["Success"] = $"Category '{model.CategoryName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: ProductCat/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var category = await _productCatService.GetProductcatByIdAsync(id);
            if (category is null)
            {
                return NotFound();
            }
            return View(category);
        }

        // GET: ProductCat/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _productCatService.GetProductcatByIdAsync(id);
            if (category is null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: ProductCat/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductCategory model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, error) = await _productCatService.UpdateProductCatAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Update failed.");
                return View(model);
            }

            TempData["Success"] = $"Category '{model.CategoryName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: ProductCat/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _productCatService.GetProductcatByIdAsync(id);
            if (category is null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: ProductCat/DeleteConfirmed/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _productCatService.DeleteProductcatAsync(id);
                TempData["Success"] = "Category deleted successfully.";
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                // Most likely cause: existing products reference this category
                // (FK_Products_ProductCategories_CategoryId is Restrict).
                _logger.LogError(ex, "Cannot delete category {Id}: referenced by products.", id);
                TempData["Error"] = "Cannot delete this category because one or more products still reference it. Reassign or delete those products first.";
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
    }
}