using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommercesite.Models
{
    /// <summary>
    /// Links a <see cref="Register"/> account to an external OAuth provider
    /// (Google, Facebook, Microsoft, GitHub, etc.). One user may have links
    /// to multiple providers.
    /// </summary>
    public class ExternalLogin
    {
        [Key]
        public int Id { get; set; }

        /// <summary>The ecommercesite user account this external login belongs to.</summary>
        [Required]
        [StringLength(36)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public Register? User { get; set; }

        /// <summary>Authentication provider name: "Google", "Facebook", "Microsoft", "GitHub", ...</summary>
        [Required]
        [StringLength(50)]
        public string Provider { get; set; } = string.Empty;

        /// <summary>The provider's unique ID for this user (the "sub" / "id" claim).</summary>
        [Required]
        [StringLength(200)]
        public string ProviderKey { get; set; } = string.Empty;

        /// <summary>The user's email as reported by the provider (snapshot at link time).</summary>
        [StringLength(150)]
        public string? ProviderEmail { get; set; }

        /// <summary>The user's display name as reported by the provider.</summary>
        [StringLength(200)]
        public string? ProviderDisplayName { get; set; }

        public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    }
}