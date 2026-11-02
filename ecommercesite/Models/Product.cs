using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommercesite.Models
{
    /// <summary>
    /// A product that belongs to a <see cref="ProductCategory"/>.
    /// </summary>
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(150)]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 1_000_000, ErrorMessage = "Price must be between 0.01 and 1,000,000")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public ProductCategory? ProductCategory { get; set; }
    }
}