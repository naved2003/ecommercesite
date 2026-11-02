using System.ComponentModel.DataAnnotations;

namespace ecommercesite.Models
{
    /// <summary>
    /// A product category. One category has many products.
    /// </summary>
    public class ProductCategory
    {
        [Key]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property (One Category → Many Products)
        public ICollection<Product> Products { get; set; } = [];
    }
}