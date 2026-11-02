using ecommercesite.Models;
using ecommercesite.ViewModels;

namespace ecommercesite.Services
{
    public interface IRegisterService
    {
        Task<IEnumerable<Register>> GetAllRegistersAsync();
        Task<Register?> GetRegisterByIdAsync(string userId);
        Task<Register?> GetRegisterByNameAsync(string firstName);
        Task<Register?> GetRegisterByEmailAsync(string email);
        Task<(bool Success, string? Error)> AddRegisterAsync(RegisterViewModel model);
        Task<(bool Success, string? Error)> UpdateRegisterAsync(Register reg);
        Task<bool> DeleteRegisterAsync(string userId);

        /// <summary>
        /// Returns all registered customer accounts (Role == "Customer").
        /// Powers the admin's Customers page.
        /// </summary>
        Task<IReadOnlyList<Register>> GetCustomerAccountsAsync();

        /// <summary>
        /// Permanently deletes a customer account. Admin-only. Orders placed
        /// by the account keep their data (FK is SET NULL) but lose the link
        /// to the deleted account. Returns false if the account doesn't exist
        /// or is an Admin account (admins cannot be removed this way).
        /// </summary>
        Task<(bool Success, string? Error)> DeleteCustomerAccountAsync(string userId);

        /// <summary>
        /// Admin-side account creation. Same validation as the self-service
        /// register flow but the admin picks the role (Customer or Admin).
        /// Hashes the password before persisting.
        /// </summary>
        Task<(bool Success, string? Error)> AddAccountAsync(
            string firstName, string lastName, string email,
            string password, string phoneNumber, string role);

        /// <summary>
        /// Admin-side account update. Modifies name, email, phone, role,
        /// and active flag. Optionally resets the password when
        /// <paramref name="newPassword"/> is non-empty.
        /// </summary>
        Task<(bool Success, string? Error)> UpdateAccountAsync(
            string userId, string firstName, string lastName,
            string email, string phoneNumber, string role,
            bool isActive, string? newPassword);
    }
}