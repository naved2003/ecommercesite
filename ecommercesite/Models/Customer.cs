using System.ComponentModel.DataAnnotations;

namespace ecommercesite.Models
{
    /// <summary>
    /// A customer record. The primary key <see cref="CustId"/> is a string
    /// (business key) so it can hold codes such as "CUST-001".
    /// </summary>
    public class Customer
    {
        [Key]
        [Required(ErrorMessage = "Customer ID is required")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Customer ID must be 3–20 characters")]
        [Display(Name = "Customer ID")]
        public string CustId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer name is required")]
        [StringLength(100, ErrorMessage = "Customer name cannot exceed 100 characters")]
        [Display(Name = "Customer Name")]
        public string CustName { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters")]
        [Display(Name = "Address")]
        public string CustAddress { get; set; } = string.Empty;
    }
}