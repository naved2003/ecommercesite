using System.ComponentModel.DataAnnotations;

namespace ecommercesite.ViewModels
{
    /// <summary>
    /// Login form DTO. Decoupled from the <see cref="ecommercesite.Models.Register"/>
    /// entity so that the login view only needs Email + Password and does not
    /// trip required-field validation on the other Register properties.
    /// </summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}