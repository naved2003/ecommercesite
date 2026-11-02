using ecommercesite.Models;
using ecommercesite.ViewModels;

namespace ecommercesite.Services
{
    /// <summary>
    /// Simulated payment gateway. Mirrors the surface of a real gateway
    /// (Stripe / Razorpay / PayPal): the controller hands off a card +
    /// amount, the service "charges" it (with Luhn validation + brand
    /// detection + simulated decline rules), records the result in the
    /// <see cref="Payment"/> table, and returns a structured result.
    /// </summary>
    /// <remarks>
    /// To swap in a real gateway, replace <see cref="PaymentService"/> with
    /// an implementation that calls Stripe.NET / Razorpay SDK / PayPal REST
    /// instead of the Luhn + test-card simulation. The controller + views
    /// stay unchanged.
    /// </remarks>
    public interface IPaymentService
    {
        /// <summary>
        /// Charges the supplied card for the given order. Records a
        /// <see cref="Payment"/> row regardless of outcome (success or
        /// failure) for audit. On success, also updates the order's
        /// <see cref="Order.Status"/> to "Confirmed".
        /// </summary>
        Task<PaymentResultDto> ProcessPaymentAsync(int orderId, CheckoutViewModel model);

        /// <summary>
        /// Returns the most recent payment row for an order (success or
        /// failure). Null if no payment has been attempted yet.
        /// </summary>
        Task<Payment?> GetLatestPaymentAsync(int orderId);

        /// <summary>
        /// Returns every payment attempt for an order, newest first.
        /// </summary>
        Task<IReadOnlyList<Payment>> GetPaymentHistoryAsync(int orderId);
    }

    /// <summary>
    /// Structured result returned by <see cref="ProcessPaymentAsync"/> so
    /// the controller doesn't have to re-fetch the Payment row.
    /// </summary>
    public class PaymentResultDto
    {
        public bool Success { get; set; }
        public string Status { get; set; } = "Pending";   // Paid | Failed
        public string? TransactionId { get; set; }
        public string? FailureReason { get; set; }
        public decimal Amount { get; set; }
        public string? CardLast4 { get; set; }
        public string? CardBrand { get; set; }
        public int OrderId { get; set; }
    }
}