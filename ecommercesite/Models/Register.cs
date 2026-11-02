using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommercesite.Models
{
    /// <summary>
    /// A registered application user. The <see cref="Password"/> field stores
    /// a PBKDF2 hash (format: iterations.saltBase64.hashBase64) — never a
    /// plain-text password.
    /// </summary>
    public class Register
    {
        [Key]
        [StringLength(36)]
        public string UserId { get; set; } = Guid.NewGuid().ToString("N");

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [NotMapped]
        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Account role: "Admin" or "Customer". The first user to register
        /// is auto-promoted to Admin; all subsequent registrations default
        /// to Customer. Stored in session on login and checked by
        /// <c>RequireAdminAttribute</c>.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Customer";

        [NotMapped]
        [Display(Name = "Full Name")]
        public string FullName => $"{FirstName} {LastName}";
    }
}
