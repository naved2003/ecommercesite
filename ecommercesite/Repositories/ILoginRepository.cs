using ecommercesite.Models;

namespace ecommercesite.Repositories
{
    /// <summary>
    /// Login-specific lookup contract. Kept narrow (only what the login flow
    /// actually needs) for clarity and to avoid pulling in CRUD surface.
    /// </summary>
    public interface ILoginRepository
    {
        Task<Register?> GetUserByEmailAsync(string email);
        Task<bool> IsActiveAsync(string email);
    }
}