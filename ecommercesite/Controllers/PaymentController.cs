using ecommercesite.Filters;
using ecommercesite.Services;
using ecommercesite.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ecommercesite.Controllers
{
    /// <summary>
    /// Customer-facing payment gateway. After a customer places an order
    /// they're redirected here to enter card details; on success the order
    /// is marked "Confirmed".
    /// </summary>
    [RequireLogin]
    public class PaymentController(
        IPaymentService paymentService,
        IOrderService orderService,
        ILogger<PaymentController> logger) : Controller
    {
        private readonly IPaymentService _paymentService = paymentService;
        private readonly IOrderService _orderService = orderService;
        private readonly ILogger<PaymentController> _logger = logger;

        // GET: /Payment/Checkout/5
        [HttpGet]
        public async Task<IActionResult> Checkout(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order is null)
            {
                return NotFound();
            }

            // Ownership: customers can only pay for their own orders.
            var role = HttpContext.Session.GetString("UserRole") ?? "Customer";
            var userId = HttpContext.Session.GetString("UserId");
            var isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
            if (!isAdmin && order.RegisterId != userId)
            {
                return Forbid();
            }

            // If the order is already paid, skip straight to the result page.
            if (await _paymentService.GetLatestPaymentAsync(id) is { Status: "Paid" } latest)
            {
                TempData["Info"] = "This order has already been paid.";
                return RedirectToAction(nameof(Result), new { id });
            }

            // Cancelled orders can't be paid.
            if (order.Status == "Cancelled")
            {
                TempData["Error"] = "Cannot pay for a cancelled order.";
                return RedirectToAction("Details", "Order", new { id });
            }

            var model = new CheckoutViewModel
            {
                OrderId = id,
                Amount = order.TotalAmount
            };

            ViewBag.OrderNumber = order.OrderId;
            ViewBag.OrderDate = order.OrderDate;
            ViewBag.ItemCount = order.Items?.Count ?? 0;

            // Show the most recent failed payment (if any) so the customer
            // sees why their last attempt was declined.
            var lastPayment = await _paymentService.GetLatestPaymentAsync(id);
            if (lastPayment is { Status: "Failed" })
            {
                ViewBag.LastFailureReason = lastPayment.FailureReason;
            }

            return View(model);
        }

        // POST: /Payment/Process/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(int id, CheckoutViewModel model)
        {
            // Force the OrderId to match the route (defensive).
            model.OrderId = id;

            if (!ModelState.IsValid)
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                ViewBag.OrderNumber = id;
                ViewBag.OrderDate = order?.OrderDate ?? DateTime.UtcNow;
                ViewBag.ItemCount = order?.Items?.Count ?? 0;
                return View("Checkout", model);
            }

            var result = await _paymentService.ProcessPaymentAsync(id, model);

            if (result.Success)
            {
                TempData["Success"] = $"Payment of {result.Amount:C} successful! Transaction ID: {result.TransactionId}";
                return RedirectToAction(nameof(Result), new { id });
            }

            // Failure — back to checkout with the error.
            TempData["Error"] = result.FailureReason ?? "Payment failed. Please try again.";
            return RedirectToAction(nameof(Checkout), new { id });
        }

        // GET: /Payment/Result/5
        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order is null)
            {
                return NotFound();
            }

            // Ownership check.
            var role = HttpContext.Session.GetString("UserRole") ?? "Customer";
            var userId = HttpContext.Session.GetString("UserId");
            var isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
            if (!isAdmin && order.RegisterId != userId)
            {
                return Forbid();
            }

            var latestPayment = await _paymentService.GetLatestPaymentAsync(id);
            var history = await _paymentService.GetPaymentHistoryAsync(id);

            ViewBag.Order = order;
            ViewBag.LatestPayment = latestPayment;
            ViewBag.History = history;

            return View();
        }

        // GET: /Payment/Retry/5
        // Shortcut that just bounces back to the checkout form so the
        // customer can try a different card.
        [HttpGet]
        public IActionResult Retry(int id)
        {
            return RedirectToAction(nameof(Checkout), new { id });
        }
    }
}