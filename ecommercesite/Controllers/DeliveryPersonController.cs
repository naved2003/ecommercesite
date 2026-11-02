using ecommercesite.Filters;
using ecommercesite.Models;
using ecommercesite.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ecommercesite.Controllers
{
    /// <summary>
    /// CRUD for delivery personnel — admin only. All actions require an
    /// authenticated admin user.
    /// </summary>
    [RequireLogin]
    [RequireAdmin]
    public class DeliveryPersonController(
        IDeliveryPersonService deliveryPersonService,
        ILogger<DeliveryPersonController> logger) : Controller
    {
        private readonly IDeliveryPersonService _deliveryPersonService = deliveryPersonService;
        private readonly ILogger<DeliveryPersonController> _logger = logger;

        private static readonly string[] StatusOptions =
            ["Available", "Busy", "Offline"];

        // GET: /DeliveryPerson?search=&page=1
        [HttpGet]
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            const int pageSize = 10;

            var result = await _deliveryPersonService.GetDeliveryPeopleAsync(search, page, pageSize);

            ViewBag.CurrentPage = result.Page;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.Search = search;

            return View(result.Items);
        }

        // GET: /DeliveryPerson/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var person = await _deliveryPersonService.GetDeliveryPersonByIdAsync(id);
            if (person is null)
            {
                return NotFound();
            }
            return View(person);
        }

        // GET: /DeliveryPerson/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.StatusOptions = StatusOptions;
            return View(new DeliveryPerson { Status = "Available", IsActive = true });
        }

        // POST: /DeliveryPerson/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DeliveryPerson model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.StatusOptions = StatusOptions;
                return View(model);
            }

            var (success, error) = await _deliveryPersonService.AddDeliveryPersonAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Create failed.");
                ViewBag.StatusOptions = StatusOptions;
                return View(model);
            }

            TempData["Success"] = $"Delivery person '{model.Name}' added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /DeliveryPerson/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var person = await _deliveryPersonService.GetDeliveryPersonByIdAsync(id);
            if (person is null)
            {
                return NotFound();
            }
            ViewBag.StatusOptions = StatusOptions;
            return View(person);
        }

        // POST: /DeliveryPerson/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DeliveryPerson model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.StatusOptions = StatusOptions;
                return View(model);
            }

            var (success, error) = await _deliveryPersonService.UpdateDeliveryPersonAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Update failed.");
                ViewBag.StatusOptions = StatusOptions;
                return View(model);
            }

            TempData["Success"] = $"Delivery person '{model.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /DeliveryPerson/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var person = await _deliveryPersonService.GetDeliveryPersonByIdAsync(id);
            if (person is null)
            {
                return NotFound();
            }
            return View(person);
        }

        // POST: /DeliveryPerson/DeleteConfirmed/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _deliveryPersonService.DeleteDeliveryPersonAsync(id);
            TempData["Success"] = "Delivery person deleted.";
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