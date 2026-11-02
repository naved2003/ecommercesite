using ecommercesite.Models;
using ecommercesite.ViewModels;

namespace ecommercesite.Repositories
{
    /// <summary>
    /// Customer-specific queries on top of the generic CRUD surface.
    /// </summary>
    public interface ICustomerRepository : IGenericRepository<Customer>
    {
        Task<Customer?> GetCustomerByIdAsync(string custId);
        Task<Customer?> GetCustomerByNameAsync(string custName);
        Task<IReadOnlyList<string>> GetCustomerNameSuggestionsAsync(string term, int max = 10);
        Task<bool> IsCustomerIdUniqueAsync(string custId);

        /// <summary>
        /// GROUP BY CustAddress, COUNT(*). Returns one row per distinct
        /// address (blank addresses are bucketed as "(no address)").
        /// </summary>
        Task<IReadOnlyList<CustomerAddressDistributionDto>> GetAddressDistributionAsync();
    }
}