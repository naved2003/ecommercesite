using ecommercesite.Models;
using ecommercesite.ViewModels;

namespace ecommercesite.Services
{
    public interface ICustomerService
    {
        Task<PagedResult<Customer>> GetCustomersAsync(string? search, int page, int pageSize);
        Task<IEnumerable<Customer>> GetAllCustomersAsync();
        Task<Customer?> GetCustomerByIdAsync(string custId);
        Task<Customer?> GetCustomerByNameAsync(string custName);
        Task<IReadOnlyList<string>> GetCustomerNameSuggestionsAsync(string term);
        Task<(bool Success, string? Error)> AddCustomerAsync(Customer customer);
        Task<(bool Success, string? Error)> UpdateCustomerAsync(Customer customer);
        Task<bool> DeleteCustomerAsync(string custId);

        /// <summary>
        /// Returns one row per distinct customer address, with the count of
        /// customers at that address. Powers the pie chart on the Customer
        /// Chart page.
        /// </summary>
        Task<IReadOnlyList<CustomerAddressDistributionDto>> GetCustomerAddressDistributionAsync();
    }
}