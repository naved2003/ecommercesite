using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommercesite.Models
{
    /// <summary>
    /// Records a payment attempt for an <see cref="Order"/>. One order may
    /// have multiple payment rows if the customer retried after a failure.
    /// Only the most recent successful payment per order is authoritative
    /// (older rows are kept for audit).
    /// </summary>
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order? Order { get; set; }

        /// <summary>The amount charged. Snapshotted at payment time so it
        /// stays correct even if the order total is later edited.</summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        /// <summary>Payment method: "Card" (Visa/Mastercard/Amex via the
        /// simulated gateway). Real gateways would add "UPI", "NetBanking",
        /// "Wallet", etc.</summary>
        [Required]
        [StringLength(20)]
        public string Method { get; set; } = "Card";

        /// <summary>
        /// Status: Pending, Paid, Failed, Refunded. Set by the payment
        /// service after the (simulated) gateway responds.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        /// <summary>Simulated transaction ID returned by the gateway.
        /// Real gateways return a long opaque string; we generate a
        /// "TXN-{guid}" so it looks realistic.</summary>
        [StringLength(64)]
        public string? TransactionId { get; set; }

        /// <summary>Last 4 digits of the card used (PCI: never store the
        /// full number). 9999 for non-card methods.</summary>
        [StringLength(4)]
        public string? CardLast4 { get; set; }

        /// <summary>Card brand detected from the number: Visa, Mastercard,
        /// Amex, Discover, Unknown.</summary>
        [StringLength(20)]
        public string? CardBrand { get; set; }

        /// <summary>Why the payment failed (decline, invalid card, etc.).
        /// Null on success.</summary>
        [StringLength(200)]
        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When the gateway confirmed the payment (UTC). Null
        /// until Status == "Paid".</summary>
        public DateTime? PaidAt { get; set; }
    }
}