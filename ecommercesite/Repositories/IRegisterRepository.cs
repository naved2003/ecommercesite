using ecommercesite.Models;

namespace ecommercesite.Repositories
{
    /// <summary>
    /// User-account repository. Extends the generic surface with user-specific
    /// lookups (by id, by name, by email, by role).
    /// </summary>
    public interface IRegisterRepository : IGenericRepository<Register>
    {
        Task<Register?> GetRegisterByIdAsync(string userId);
        Task<Register?> GetRegisterByNameAsync(string firstName);
        Task<Register?> GetRegisterByEmailAsync(string email);

        /// <summary>
        /// Returns true when no account (other than <paramref name="excludeId"/>
        /// when supplied) already uses the supplied email. Pass the userId
        /// being edited to skip its own row.
        /// </summary>
        Task<bool> IsEmailUniqueAsync(string email, string? excludeId = null);

        /// <summary>
        /// Returns all user accounts with the specified role (e.g. "Customer"
        /// or "Admin"), ordered by name. Used by the admin Customers page
        /// to list only registered customer accounts.
        /// </summary>
        Task<IReadOnlyList<Register>> GetByRoleAsync(string role);
    }
}