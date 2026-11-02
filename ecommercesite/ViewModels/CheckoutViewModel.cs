using System.ComponentModel.DataAnnotations;

namespace ecommercesite.ViewModels
{
    /// <summary>
    /// Card-details form for the checkout page. The card number is the
    /// full PAN (primary account number) so we can run a Luhn check + detect
    /// the brand, but only the last 4 digits are persisted to the
    /// <see cref="ecommercesite.Models.Payment"/> row.
    /// </summary>
    public class CheckoutViewModel
    {
        public int OrderId { get; set; }

        [Display(Name = "Amount Due")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Cardholder name is required")]
        [StringLength(100)]
        [Display(Name = "Cardholder Name")]
        public string CardName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Card number is required")]
        [StringLength(19, MinimumLength = 13,
            ErrorMessage = "Card number must be 13–19 digits")]
        [Display(Name = "Card Number")]
        [RegularExpression(@"^[0-9 ]+$",
            ErrorMessage = "Card number may only contain digits and spaces")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Expiry date is required")]
        [StringLength(5,
            ErrorMessage = "Expiry must be in MM/YY format")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$",
            ErrorMessage = "Expiry must be in MM/YY format (e.g. 12/27)")]
        [Display(Name = "Expiry (MM/YY)")]
        public string Expiry { get; set; } = string.Empty;

        [Required(ErrorMessage = "CVV is required")]
        [StringLength(4, MinimumLength = 3,
            ErrorMessage = "CVV must be 3 or 4 digits")]
        [RegularExpression(@"^\d{3,4}$",
            ErrorMessage = "CVV must be 3 or 4 digits")]
        [Display(Name = "CVV")]
        public string Cvv { get; set; } = string.Empty;
    }
}