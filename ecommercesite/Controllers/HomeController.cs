using ecommercesite.Filters;
using ecommercesite.Models;
using ecommercesite.Services;
using ecommercesite.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ecommercesite.Controllers
{
    /// <summary>
    /// Admin customer-accounts page. Lists only registered user accounts.
    /// Admins can create, view, edit, and remove accounts. Customers
    /// self-register via RegisterController1 — admins use this page to
    /// manage all accounts (including promoting someone to Admin).
    /// </summary>
    [RequireLogin]
    [RequireAdmin]
    public class HomeController(
        ILogger<HomeController> logger,
        IRegisterService registerService,
        IOrderService orderService) : Controller
    {
        private readonly ILogger<HomeController> _logger = logger;
        private readonly IRegisterService _registerService = registerService;
        private readonly IOrderService _orderService = orderService;

        // Roles available in the create/edit dropdowns.
        private static readonly string[] RoleOptions = ["Customer", "Admin"];

        // GET: Home/Index?search=
        // Lists all registered customer accounts (Role == "Customer").
        public async Task<IActionResult> Index(string? search)
        {
            var customers = await _registerService.GetCustomerAccountsAsync();

            // Client-side filter (the list is small enough for in-memory search).
            var term = search?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(term))
            {
                customers = [.. customers
                    .Where(c => (c.FirstName + " " + c.LastName).Contains(term, StringComparison.InvariantCultureIgnoreCase)
                                || c.Email.Contains(term, StringComparison.InvariantCultureIgnoreCase)
                                || c.PhoneNumber.Contains(term, StringComparison.InvariantCultureIgnoreCase))];
            }

            ViewBag.Search = search;
            return View(customers);
        }

        // GET: Home/Create
        // Admin creates a new account (Customer or Admin).
        public IActionResult Create()
        {
            ViewBag.RoleOptions = RoleOptions;
            return View(new AdminAccountViewModel { Role = "Customer", IsActive = true });
        }

        // POST: Home/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RoleOptions = RoleOptions;
                return View(model);
            }

            var (success, error) = await _registerService.AddAccountAsync(
                model.FirstName, model.LastName, model.Email,
                model.Password, model.PhoneNumber, model.Role);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Create failed.");
                ViewBag.RoleOptions = RoleOptions;
                return View(model);
            }

            TempData["Success"] = $"{model.Role} account '{model.FirstName} {model.LastName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Home/Edit/{userId}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var account = await _registerService.GetRegisterByIdAsync(id);
            if (account is null)
            {
                return NotFound();
            }

            var model = new AdminAccountViewModel
            {
                UserId = account.UserId,
                FirstName = account.FirstName,
                LastName = account.LastName,
                Email = account.Email,
                PhoneNumber = account.PhoneNumber,
                Role = account.Role,
                IsActive = account.IsActive,
                // Password is never sent back to the form — leave blank.
                Password = string.Empty,
                NewPassword = string.Empty
            };

            ViewBag.RoleOptions = RoleOptions;
            return View(model);
        }

        // POST: Home/Edit/{userId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RoleOptions = RoleOptions;
                return View(model);
            }

            if (string.IsNullOrEmpty(model.UserId))
            {
                return NotFound();
            }

            var (success, error) = await _registerService.UpdateAccountAsync(
                model.UserId,
                model.FirstName, model.LastName, model.Email,
                model.PhoneNumber, model.Role, model.IsActive,
                model.NewPassword);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Update failed.");
                ViewBag.RoleOptions = RoleOptions;
                return View(model);
            }

            TempData["Success"] = $"Account '{model.FirstName} {model.LastName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Home/Details/{userId}
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var account = await _registerService.GetRegisterByIdAsync(id);
            if (account is null)
            {
                return NotFound();
            }

            // Fetch this customer's orders (matches CustId or RegisterId).
            var orders = await _orderService.GetOrdersByCustomerAsync(id);

            ViewBag.Orders = orders;
            return View(account);
        }

        // GET: Home/Delete/{userId}
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var account = await _registerService.GetRegisterByIdAsync(id);
            if (account is null)
            {
                return NotFound();
            }

            return View(account);
        }

        // POST: Home/DeleteConfirmed/{userId}
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var (success, error) = await _registerService.DeleteCustomerAccountAsync(id);
            if (!success)
            {
                TempData["Error"] = error ?? "Could not delete account.";
            }
            else
            {
                TempData["Success"] = "Account removed successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
