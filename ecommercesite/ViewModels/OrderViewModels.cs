using System.ComponentModel.DataAnnotations;

namespace ecommercesite.ViewModels
{
    /// <summary>
    /// One row in the "Create Order" form.
    /// </summary>
    public class CreateOrderItemViewModel
    {
        [Required(ErrorMessage = "Product is required")]
        [Display(Name = "Product")]
        public int ProductId { get; set; }

        [Range(1, 10_000, ErrorMessage = "Quantity must be between 1 and 10,000")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; } = 1;
    }

    /// <summary>
    /// Top-level form model for the Create Order page. Captures the customer
    /// plus one or more line items in a single POST.
    /// </summary>
    /// <remarks>
    /// <para><b>CustId</b> is only filled in by the admin Create flow (where
    /// the admin picks a Customer from a dropdown). The customer-facing
    /// PlaceOrder flow leaves <c>CustId</c> empty and ties the order to the
    /// logged-in user's <c>RegisterId</c> instead — so <c>CustId</c> must
    /// NOT be [Required] on this shared view model.</para>
    /// </remarks>
    public class CreateOrderViewModel
    {
        [Display(Name = "Customer")]
        public string? CustId { get; set; }

        [Display(Name = "Shipping Address")]
        [StringLength(500)]
        public string? ShippingAddress { get; set; }

        [Required(ErrorMessage = "Add at least one product")]
        [MinLength(1, ErrorMessage = "Add at least one product")]
        public List<CreateOrderItemViewModel> Items { get; set; } = [];
    }
}