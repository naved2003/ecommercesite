using ecommercesite.Models;
using ecommercesite.Repositories;
using ecommercesite.ViewModels;

namespace ecommercesite.Services
{
    /// <summary>
    /// Simulated payment gateway. Validates the card (Luhn check, expiry
    /// in the future, CVV length matching brand), then applies a set of
    /// "test card" rules to decide success vs. decline. Persists every
    /// attempt to the <see cref="Payment"/> table for audit.
    /// </summary>
    /// <remarks>
    /// <para><b>Test cards</b> (simulated, no real money moves):</para>
    /// <list type="bullet">
    ///   <item><c>4242 4242 4242 4242</c> — Visa, always succeeds</item>
    ///   <item><c>5555 5555 5555 4444</c> — Mastercard, always succeeds</item>
    ///   <item><c>3782 822463 10005</c>  — Amex, always succeeds</item>
    ///   <item><c>4000 0000 0000 0002</c> — Visa, always declines (insufficient funds)</item>
    ///   <item><c>4000 0000 0000 0119</c> — Visa, always declines (suspected fraud)</item>
    ///   <item>Any other valid-Luhn card — succeeds (so testers can use
    ///         any generated test number)</item>
    /// </list>
    /// <para>To swap in a real gateway, replace the body of
    /// <see cref="ProcessPaymentAsync"/> with a call to Stripe.NET /
    /// Razorpay SDK / PayPal REST. The DTO + repository stay the same.</para>
    /// </remarks>
    public class PaymentService(
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository,
        ILogger<PaymentService> logger) : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository = paymentRepository;
        private readonly IOrderRepository _orderRepository = orderRepository;
        private readonly ILogger<PaymentService> _logger = logger;

        public async Task<PaymentResultDto> ProcessPaymentAsync(int orderId, CheckoutViewModel model)
        {
            // ---- Fetch the order (tracked) so we can mutate its status ----
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order is null)
            {
                return new PaymentResultDto
                {
                    Success = false,
                    Status = "Failed",
                    FailureReason = "Order not found.",
                    OrderId = orderId
                };
            }

            // Don't allow double-charging an already-paid order.
            if (await _paymentRepository.HasSuccessfulPaymentAsync(orderId))
            {
                return new PaymentResultDto
                {
                    Success = false,
                    Status = "Failed",
                    FailureReason = "This order has already been paid.",
                    Amount = order.TotalAmount,
                    OrderId = orderId
                };
            }

            // ---- Normalize the card number (strip spaces) ---------------
            var digits = new string([.. model.CardNumber.Where(char.IsDigit)]);
            var last4 = digits.Length >= 4 ? digits[^4..] : digits;
            var brand = DetectBrand(digits);

            // ---- Validate card -------------------------------------------
            // Luhn check — catches typos in the card number.
            if (!IsValidLuhn(digits))
            {
                return await RecordFailureAsync(order, model.Amount, last4, brand,
                    "Invalid card number (failed Luhn check).");
            }

            // Expiry must be in the future.
            if (!TryParseExpiry(model.Expiry, out var expiryYear, out var expiryMonth))
            {
                return await RecordFailureAsync(order, model.Amount, last4, brand,
                    "Invalid expiry date.");
            }
            var now = DateTime.UtcNow;
            var expiryEndOfMonth = new DateTime(expiryYear, expiryMonth,
                DateTime.DaysInMonth(expiryYear, expiryMonth), 23, 59, 59, DateTimeKind.Utc);
            if (expiryEndOfMonth < now)
            {
                return await RecordFailureAsync(order, model.Amount, last4, brand,
                    "Card has expired.");
            }

            // CVV length must match brand (3 for Visa/MC/Discover, 4 for Amex).
            var expectedCvvLength = brand == "Amex" ? 4 : 3;
            if (model.Cvv.Length != expectedCvvLength)
            {
                return await RecordFailureAsync(order, model.Amount, last4, brand,
                    $"CVV must be {expectedCvvLength} digits for {brand} cards.");
            }

            // ---- Simulated gateway: apply test-card rules ----------------
            // The artificial delay mimics a real gateway round-trip so the
            // UI's "Processing…" spinner has something to show.
            await Task.Delay(Random.Shared.Next(800, 1800));

            var (success, reason) = SimulateGatewayResponse(digits);

            // ---- Record the outcome --------------------------------------
            var txnId = success ? $"TXN-{Guid.NewGuid():N}".ToUpperInvariant()[..24] : null;

            var payment = new Payment
            {
                OrderId = orderId,
                Amount = model.Amount,
                Method = "Card",
                Status = success ? "Paid" : "Failed",
                TransactionId = txnId,
                CardLast4 = last4,
                CardBrand = brand,
                FailureReason = success ? null : reason,
                CreatedAt = DateTime.UtcNow,
                PaidAt = success ? DateTime.UtcNow : null
            };

            await _paymentRepository.AddAsync(payment);

            // On success, also bump the order's main Status to "Confirmed"
            // (from "Pending"). This is the same hook a real gateway's
            // webhook would hit.
            if (success)
            {
                order.Status = "Confirmed";
            }

            await _paymentRepository.SaveAsync();

            _logger.LogInformation("Payment {Status} for order {OrderId}: {CardBrand} ****{Last4} ${Amount} txn={TxnId}",
                payment.Status, orderId, brand, last4, payment.Amount, txnId ?? "—");

            return new PaymentResultDto
            {
                Success = success,
                Status = payment.Status,
                TransactionId = txnId,
                FailureReason = success ? null : reason,
                Amount = payment.Amount,
                CardLast4 = last4,
                CardBrand = brand,
                OrderId = orderId
            };
        }

        public async Task<Payment?> GetLatestPaymentAsync(int orderId)
        {
            return await _paymentRepository.GetLatestPaymentForOrderAsync(orderId);
        }

        public async Task<IReadOnlyList<Payment>> GetPaymentHistoryAsync(int orderId)
        {
            return await _paymentRepository.GetPaymentsForOrderAsync(orderId);
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private async Task<PaymentResultDto> RecordFailureAsync(
            Order order, decimal amount, string last4, string brand, string reason)
        {
            // Persist the failed attempt so the audit trail is complete.
            var payment = new Payment
            {
                OrderId = order.OrderId,
                Amount = amount,
                Method = "Card",
                Status = "Failed",
                CardLast4 = last4,
                CardBrand = brand,
                FailureReason = reason,
                CreatedAt = DateTime.UtcNow
            };
            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveAsync();

            _logger.LogWarning("Payment failed for order {OrderId}: {Reason}", order.OrderId, reason);

            return new PaymentResultDto
            {
                Success = false,
                Status = "Failed",
                FailureReason = reason,
                Amount = amount,
                CardLast4 = last4,
                CardBrand = brand,
                OrderId = order.OrderId
            };
        }

        /// <summary>Detects the card brand from the IIN (first few digits).</summary>
        private static string DetectBrand(string digits)
        {
            if (digits.Length < 2) return "Unknown";
            if (digits[0] == '4') return "Visa";
            if (digits.StartsWith("34") || digits.StartsWith("37")) return "Amex";
            if (digits.Length >= 2 && int.Parse(digits[..2]) is var s && s >= 51 && s <= 55) return "Mastercard";
            if (digits.StartsWith("6011") || digits.StartsWith("65")) return "Discover";
            if (digits.Length >= 6 && digits.StartsWith("622126", StringComparison.Ordinal)) return "Discover";
            return "Unknown";
        }

        /// <summary>
        /// Luhn algorithm — the checksum used by all real card numbers.
        /// Returns true if <paramref name="digits"/> passes the check.
        /// </summary>
        private static bool IsValidLuhn(string digits)
        {
            if (digits.Length < 13 || digits.Any(c => !char.IsDigit(c))) return false;

            int sum = 0;
            bool doubleDigit = false;
            for (int i = digits.Length - 1; i >= 0; i--)
            {
                int d = digits[i] - '0';
                if (doubleDigit)
                {
                    d *= 2;
                    if (d > 9) d -= 9;
                }
                sum += d;
                doubleDigit = !doubleDigit;
            }
            return sum % 10 == 0;
        }

        /// <summary>Parses "MM/YY" into year + month. Returns false on bad format.</summary>
        private static bool TryParseExpiry(string expiry, out int year, out int month)
        {
            year = 0; month = 0;
            var parts = expiry.Split('/');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out month)) return false;
            if (!int.TryParse(parts[1], out var yy)) return false;
            if (month < 1 || month > 12) return false;
            // YY → 20YY
            year = 2000 + yy;
            return true;
        }

        /// <summary>
        /// Applies the simulated gateway's decline rules based on the card
        /// number. Test card numbers always return a deterministic result;
        /// any other valid-Luhn card succeeds.
        /// </summary>
        private static (bool Success, string? Reason) SimulateGatewayResponse(string digits)
        {
            // Always-success test cards
            if (digits == "4242424242424242"   // Visa
                || digits == "5555555555554444" // Mastercard
                || digits == "378282246310005"  // Amex
                || digits == "6011111111111117") // Discover
            {
                return (true, null);
            }

            // Always-decline test cards
            if (digits == "4000000000000002")
                return (false, "Card declined: insufficient funds.");
            if (digits == "4000000000000119")
                return (false, "Card declined: suspected fraud. Contact your bank.");
            if (digits == "4000000000000069")
                return (false, "Card declined: expired card.");
            if (digits == "4000000000000127")
                return (false, "Card declined: incorrect CVV.");
            if (digits == "4000000000000119")
                return (false, "Card declined: suspected fraud.");

            // Any other valid-Luhn card: succeed (so testers can use
            // generated test numbers from any online Luhn generator).
            return (true, null);
        }
    }
}