using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommercesite.Models
{
    /// <summary>
    /// An order placed by a <see cref="Customer"/>. Contains many
    /// <see cref="OrderItem"/>s. The customer link uses the string CustId
    /// to match the (unfortunately-stringy) Customer primary key.
    /// </summary>
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        /// <summary>
        /// Optional FK to the legacy <see cref="Customer"/> entity. Only set
        /// for admin-created orders that pick a Customer from the dropdown.
        /// Customer-placed orders leave this null and use <see cref="RegisterId"/>
        /// instead. MUST be nullable so the FK constraint doesn't reject
        /// customer-placed orders.
        /// </summary>
        [StringLength(20)]
        [Display(Name = "Customer ID")]
        public string? CustId { get; set; }

        [ForeignKey(nameof(CustId))]
        public Customer? Customer { get; set; }

        /// <summary>
        /// Optional FK to the <see cref="Register"/> (user account) that
        /// placed this order. Set when a customer places an order through
        /// the customer-facing flow. Null for admin-created orders (which
        /// use <see cref="CustId"/> instead).
        /// </summary>
        [StringLength(36)]
        public string? RegisterId { get; set; }

        [ForeignKey(nameof(RegisterId))]
        public Register? Register { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Order status: Pending, Confirmed, Shipped, Delivered, Cancelled.
        /// Stored as a free-form string so callers can introduce new
        /// statuses without a migration.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [StringLength(500)]
        [Display(Name = "Shipping Address")]
        public string? ShippingAddress { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Order Total")]
        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // -------------------------------------------------------------------
        // Delivery assignment
        // -------------------------------------------------------------------

        /// <summary>
        /// Optional FK to the <see cref="DeliveryPerson"/> assigned to fulfil
        /// this order. Null when no delivery person has been assigned yet.
        /// </summary>
        public int? DeliveryPersonId { get; set; }

        [ForeignKey(nameof(DeliveryPersonId))]
        public DeliveryPerson? DeliveryPerson { get; set; }

        /// <summary>
        /// Delivery-specific status, tracked separately from the order
        /// status: Not Assigned, Assigned, Out for Delivery, Delivered,
        /// Failed. Null until a delivery person is assigned.
        /// </summary>
        [StringLength(20)]
        public string? DeliveryStatus { get; set; }

        /// <summary>
        /// When the order was actually delivered (UTC). Null until
        /// DeliveryStatus becomes "Delivered".
        /// </summary>
        public DateTime? DeliveredAt { get; set; }

        // Navigation Property (One Order → Many OrderItems)
        public ICollection<OrderItem> Items { get; set; } = [];
    }
}