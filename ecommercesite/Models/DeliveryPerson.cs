using System.ComponentModel.DataAnnotations;

namespace ecommercesite.Models
{
    /// <summary>
    /// A delivery person who fulfills <see cref="Order"/> shipments.
    /// </summary>
    public class DeliveryPerson
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(150)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(50)]
        [Display(Name = "Vehicle Number")]
        public string? VehicleNumber { get; set; }

        /// <summary>
        /// Availability: Available, Busy, Offline. Free-form string so new
        /// statuses can be added without a migration.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Available";

        [StringLength(100)]
        [Display(Name = "Assigned Area")]
        public string? AssignedArea { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation: orders currently assigned to this delivery person.
        public ICollection<Order> AssignedOrders { get; set; } = [];
    }
}