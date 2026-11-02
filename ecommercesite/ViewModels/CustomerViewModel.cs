using System.ComponentModel.DataAnnotations;

namespace ecommercesite.ViewModels
{
    /// <summary>
    /// Lightweight customer DTO for views (no behavior, no EF attributes).
    /// </summary>
    public class CustomerViewModel
    {
        [Display(Name = "Customer ID")]
        public string CustId { get; set; } = string.Empty;

        [Display(Name = "Customer Name")]
        public string CustName { get; set; } = string.Empty;

        [Display(Name = "Address")]
        public string CustAddress { get; set; } = string.Empty;
    }
}