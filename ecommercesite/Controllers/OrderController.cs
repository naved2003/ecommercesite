using ecommercesite.Filters;
using ecommercesite.Models;
using ecommercesite.Services;
using ecommercesite.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics;

namespace ecommercesite.Controllers
{
    /// <summary>
    /// All order actions (list, create, details, edit, delete, status
    /// update, yearly report) require the user to be logged in. Anonymous
    /// visitors are redirected to /Account/Index with a returnUrl so they
    /// come back here after a successful login.
    /// </summary>
    [RequireLogin]
    public class OrderController(
        IOrderService orderService,
        ICustomerService customerService,
        IProductService productService,
        IDeliveryPersonService deliveryPersonService,
        ILogger<OrderController> logger) : Controller
    {
        private readonly IOrderService _orderService = orderService;
        private readonly ICustomerService _customerService = customerService;
        private readonly IProductService _productService = productService;
        private readonly IDeliveryPersonService _deliveryPersonService = deliveryPersonService;
        private readonly ILogger<OrderController> _logger = logger;

        private static readonly string[] StatusOptions =
            ["Pending", "Confirmed", "Shipped", "Delivered", "Cancelled"];

        private static readonly string[] DeliveryStatusOptions =
            ["Assigned", "Out for Delivery", "Delivered", "Failed"];

        // GET: /Order?search=&startDate=&endDate=&page=1
        [HttpGet]
        public async Task<IActionResult> Index(string? search, DateTime? startDate, DateTime? endDate, int page = 1)
        {
            const int pageSize = 10;

            // Guard: if the user somehow picks an end date before the start
            // date, swap them so the query doesn't return zero rows silently.
            if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
            {
                (startDate, endDate) = (endDate, startDate);
            }

            // Role-based scoping: customers see only their own orders;
            // admins see all orders.
            var role = HttpContext.Session.GetString("UserRole") ?? "Customer";
            var userId = HttpContext.Session.GetString("UserId");
            var registerIdFilter = role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                ? null
                : userId;

            var result = await _orderService.GetOrdersAsync(search, startDate, endDate, page, pageSize, registerIdFilter);

            ViewBag.CurrentPage = result.Page;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.Search = search;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.IsAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

            // Charts are admin-only — customers get a simpler list view.
            if (ViewBag.IsAdmin)
            {
                var locationDistribution = await _orderService.GetOrderLocationDistributionAsync(startDate, endDate);
                var customerDistribution = await _orderService.GetOrderCustomerDistributionAsync(startDate, endDate);
                var locationCustomerBreakdown = await _orderService.GetOrderCustomerBreakdownByLocationAsync(startDate, endDate);

                ViewBag.LocationDistribution = locationDistribution;
                ViewBag.CustomerDistribution = customerDistribution;
                ViewBag.LocationCustomerBreakdown = locationCustomerBreakdown;
            }

            return View(result.Items);
        }

        // GET: /Order/Details/5
        // GET: /Order/YearlyReport
        [HttpGet]
        [RequireAdmin]
        public async Task<IActionResult> YearlyReport()
        {
            var report = await _orderService.GetOrderYearlyReportAsync();

            // KPI cards at the top of the report.
            ViewBag.TotalOrders = report.Sum(r => r.OrderCount);
            ViewBag.TotalRevenue = report.Sum(r => r.TotalRevenue);
            ViewBag.AvgOrderValue = ViewBag.TotalOrders > 0
                ? (decimal)ViewBag.TotalRevenue / (int)ViewBag.TotalOrders
                : 0m;
            var best = report.MaxBy(r => r.TotalRevenue);
            ViewBag.BestYear = best?.Year.ToString() ?? "—";
            ViewBag.BestYearRevenue = best?.TotalRevenue ?? 0m;

            return View(report);
        }

        // GET: /Order/TopProducts?year=2026
        // Admin-only. Two reports on one page:
        //   1. Max product purchased by each customer (all-time)
        //   2. Max product ordered in the selected year
        [HttpGet]
        [RequireAdmin]
        public async Task<IActionResult> TopProducts(int? year)
        {
            // Default year = the year of the most recent order (so the page
            // shows something meaningful on first load).
            var yearly = await _orderService.GetOrderYearlyReportAsync();
            var availableYears = yearly.Select(y => y.Year).OrderByDescending(y => y).ToList();
            if (availableYears.Count == 0)
            {
                availableYears = [DateTime.UtcNow.Year];
            }

            var selectedYear = year ?? availableYears[0];
            if (!availableYears.Contains(selectedYear))
            {
                availableYears.Insert(0, selectedYear);
                availableYears = [.. availableYears.Distinct().OrderByDescending(y => y)];
            }

            var topByCustomer = await _orderService.GetTopProductPerCustomerAsync();
            var topInYear = await _orderService.GetTopProductInYearAsync(selectedYear);

            ViewBag.AvailableYears = availableYears;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.TopByCustomer = topByCustomer;
            ViewBag.TopInYear = topInYear;

            // Headline KPI for the year.
            var yearWinner = topInYear.FirstOrDefault();
            ViewBag.YearTopProduct = yearWinner?.ProductName ?? "—";
            ViewBag.YearTopQty = yearWinner?.TotalQuantity ?? 0;
            ViewBag.YearTopRevenue = yearWinner?.TotalRevenue ?? 0m;
            ViewBag.YearProductCount = topInYear.Count;
            ViewBag.YearTotalQty = topInYear.Sum(r => r.TotalQuantity);
            ViewBag.YearTotalRevenue = topInYear.Sum(r => r.TotalRevenue);

            return View();
        }

        // GET: /Order/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order is null)
            {
                return NotFound();
            }

            // Ownership check: customers can only view their own orders.
            var role = HttpContext.Session.GetString("UserRole") ?? "Customer";
            var userId = HttpContext.Session.GetString("UserId");
            var isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
            if (!isAdmin && order.RegisterId != userId)
            {
                return Forbid();
            }

            ViewBag.IsAdmin = isAdmin;
            ViewBag.StatusOptions = StatusOptions;
            ViewBag.DeliveryStatusOptions = DeliveryStatusOptions;

            // Delivery-person dropdown is admin-only.
            if (isAdmin)
            {
                var available = await _deliveryPersonService.GetAvailableDeliveryPeopleAsync();
                var deliveryChoices = available
                    .Select(d => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = $"{d.Name} — {d.Phone}{(string.IsNullOrEmpty(d.AssignedArea) ? "" : $" ({d.AssignedArea})")}"
                    })
                    .ToList();

                if (order.DeliveryPerson is not null
                    && !deliveryChoices.Any(c => c.Value == order.DeliveryPerson.Id.ToString()))
                {
                    var dp = order.DeliveryPerson;
                    deliveryChoices.Insert(0, new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = dp.Id.ToString(),
                        Text = $"{dp.Name} — {dp.Phone} (currently {dp.Status})"
                    });
                }

                ViewBag.DeliveryPeople = deliveryChoices;
            }

            return View(order);
        }

        // -------------------------------------------------------------------
        // Customer-facing order placement (no customer dropdown — the order
        // is auto-tied to the logged-in user's Register account).
        // -------------------------------------------------------------------

        // GET: /Order/PlaceOrder
        [HttpGet]
        public async Task<IActionResult> PlaceOrder()
        {
            await PopulateProductsForOrder();
            return View(new CreateOrderViewModel
            {
                Items = [new()]
            });
        }

        // POST: /Order/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CreateOrderViewModel model)
        {
            // Strip out any empty rows before validating.
            model.Items = [.. model.Items.Where(i => i.ProductId > 0 && i.Quantity > 0)];

            if (!ModelState.IsValid)
            {
                await PopulateProductsForOrder();
                return View(model);
            }

            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index", "Account");
            }

            var (orderId, success, error) = await _orderService.CreateOrderForCustomerAsync(
                userId, model.ShippingAddress, model.Items);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Order failed.");
                await PopulateProductsForOrder();
                return View(model);
            }

            TempData["Success"] = $"Order #{orderId} placed successfully! Please complete your payment.";
            return RedirectToAction("Checkout", "Payment", new { id = orderId });
        }

        // -------------------------------------------------------------------
        // Customer-facing "My Products" report page.
        // Shows the customer's ordered products filtered by date range +
        // product name, with a pie chart of product distribution.
        // -------------------------------------------------------------------

        // GET: /Order/MyProducts?startDate=&endDate=&productName=
        [HttpGet]
        public async Task<IActionResult> MyProducts(DateTime? startDate, DateTime? endDate, string? productName)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index", "Account");
            }

            // Guard: swap if end < start so the query isn't empty silently.
            if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
            {
                (startDate, endDate) = (endDate, startDate);
            }

            var productOrders = await _orderService.GetCustomerProductOrdersAsync(
                userId, startDate, endDate, productName);

            // The pie chart always reflects the date-range filter (ignores
            // the product-name search box so you can see the full product
            // breakdown even while searching for one product in the table).
            var distribution = await _orderService.GetCustomerProductDistributionAsync(
                userId, startDate, endDate);

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.ProductName = productName;
            ViewBag.TotalItems = productOrders.Count;
            ViewBag.TotalQuantity = productOrders.Sum(r => r.Quantity);
            ViewBag.TotalSpent = productOrders.Sum(r => r.LineTotal);

            ViewBag.ProductOrders = productOrders;
            ViewBag.Distribution = distribution;

            return View();
        }

        // POST: /Order/AssignDelivery/5
        // Body: deliveryPersonId (int? — 0 / null = unassign)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAdmin]
        public async Task<IActionResult> AssignDelivery(int id, int? deliveryPersonId)
        {
            try
            {
                var (success, error) = await _orderService.AssignDeliveryPersonAsync(id, deliveryPersonId);
                if (!success)
                {
                    TempData["Error"] = error;
                }
                else if (deliveryPersonId is null or 0)
                {
                    TempData["Success"] = $"Delivery person unassigned from order #{id}.";
                }
                else
                {
                    TempData["Success"] = $"Delivery person assigned to order #{id}.";
                }
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                _logger.LogError(ex, "DB error assigning delivery person to order {OrderId}", id);
                TempData["Error"] = "A database error occurred while assigning the delivery person.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Order/UpdateDeliveryStatus/5
        // Body: deliveryStatus (string)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAdmin]
        public async Task<IActionResult> UpdateDeliveryStatus(int id, string deliveryStatus)
        {
            try
            {
                var (success, error) = await _orderService.UpdateDeliveryStatusAsync(id, deliveryStatus);
                if (!success)
                {
                    TempData["Error"] = error;
                }
                else
                {
                    TempData["Success"] = $"Order #{id} delivery status updated to '{deliveryStatus}'.";
                }
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                _logger.LogError(ex, "DB error updating delivery status for order {OrderId}", id);
                TempData["Error"] = "A database error occurred while updating the delivery status.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Order/Create
        [HttpGet]
        [RequireAdmin]
        public async Task<IActionResult> Create()
        {
            await PopulateCustomersAndProducts();
            return View(new CreateOrderViewModel
            {
                Items = [new()]
            });
        }

        // POST: /Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAdmin]
        public async Task<IActionResult> Create(CreateOrderViewModel model)
        {
            // Strip out any empty rows before validating.
            model.Items = [.. model.Items.Where(i => i.ProductId > 0 && i.Quantity > 0)];

            if (!ModelState.IsValid)
            {
                await PopulateCustomersAndProducts(model.CustId);
                return View(model);
            }

            var (orderId, success, error) = await _orderService.CreateOrderAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Create failed.");
                await PopulateCustomersAndProducts(model.CustId);
                return View(model);
            }

            TempData["Success"] = $"Order #{orderId} created successfully.";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // GET: /Order/Delete/5
        [HttpGet]
        [RequireAdmin]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order is null)
            {
                return NotFound();
            }
            return View(order);
        }

        // POST: /Order/DeleteConfirmed/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        [RequireAdmin]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _orderService.DeleteOrderAsync(id);
            TempData["Success"] = $"Order #{id} deleted.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Order/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAdmin]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            try
            {
                var (success, error) = await _orderService.UpdateStatusAsync(id, status);
                if (!success)
                {
                    TempData["Error"] = error;
                }
                else
                {
                    TempData["Success"] = $"Order #{id} status updated to '{status}'.";
                }
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                _logger.LogError(ex, "DB error updating status for order {OrderId}", id);
                TempData["Error"] = "A database error occurred while updating the order status. Please try again.";
            }
            return RedirectToAction(nameof(Details), new { id });
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
        private async Task PopulateCustomersAndProducts(string? selectedCustId = null)
        {
            var customers = await _customerService.GetAllCustomersAsync();
            ViewBag.Customers = customers
                .Select(c => new SelectListItem
                {
                    Value = c.CustId,
                    Text = $"{c.CustId} — {c.CustName}",
                    Selected = selectedCustId != null && c.CustId == selectedCustId
                })
                .OrderBy(c => c.Text)
                .ToList();

            var products = await _productService.GetProductsAsync(search: null, page: 1, pageSize: 100);
            ViewBag.Products = products.Items
                .Select(p => new SelectListItem
                {
                    Value = p.ProductId.ToString(),
                    Text = $"{p.ProductName} — ${p.Price:0.00}"
                })
                .OrderBy(p => p.Text)
                .ToList();
        }

        /// <summary>
        /// Customer-facing helper: populates only the product dropdown (no
        /// customer dropdown — the order is auto-tied to the logged-in user).
        /// </summary>
        private async Task PopulateProductsForOrder()
        {
            var products = await _productService.GetProductsAsync(search: null, page: 1, pageSize: 100);
            ViewBag.Products = products.Items
                .Select(p => new SelectListItem
                {
                    Value = p.ProductId.ToString(),
                    Text = $"{p.ProductName} — ${p.Price:0.00}"
                })
                .OrderBy(p => p.Text)
                .ToList();
        }
    }
}
