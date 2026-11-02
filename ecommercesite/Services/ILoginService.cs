using ecommercesite.Models;

namespace ecommercesite.Services
{
    public interface ILoginService
    {
        /// <summary>
        /// Attempts to log in a user. Returns the user entity on success or
        /// <c>null</c> on bad credentials / inactive account.
        /// </summary>
        Task<Register?> LoginAsync(string email, string password);
    }
}