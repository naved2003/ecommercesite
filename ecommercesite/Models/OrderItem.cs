using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommercesite.Models
{
    /// <summary>
    /// One line item on an <see cref="Order"/>. Snapshots the product name
    /// and unit price at the moment the order was placed so historical
    /// orders stay accurate even if the product is later renamed or its
    /// price changes.
    /// </summary>
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order? Order { get; set; }

        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Range(0.01, 1_000_000)]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        [Range(1, 10_000)]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        [NotMapped]
        [Display(Name = "Line Total")]
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
