using ecommercesite.Models;
using ecommercesite.Repositories;
using ecommercesite.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ecommercesite.Services
{
    public class CustomerService(
        ICustomerRepository customerRepository,
        ILogger<CustomerService> logger) : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository = customerRepository;
        private readonly ILogger<CustomerService> _logger = logger;

        public async Task<PagedResult<Customer>> GetCustomersAsync(string? search, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

            var term = search?.Trim();
            Expression<Func<Customer, bool>>? filter = null;
            if (!string.IsNullOrEmpty(term))
            {
                filter = c => c.CustName.Contains(term) || c.CustId.Contains(term);
            }

            var total = await _customerRepository.CountAsync(filter);
            var items = await _customerRepository.GetAllAsync(
                filter: filter,
                orderBy: q => q.OrderBy(c => c.CustName),
                skip: (page - 1) * pageSize,
                take: pageSize,
                asNoTracking: true);

            return new PagedResult<Customer>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            return await _customerRepository.GetAllAsync(
                orderBy: q => q.OrderBy(c => c.CustName));
        }

        public async Task<Customer?> GetCustomerByIdAsync(string custId)
        {
            return await _customerRepository.GetCustomerByIdAsync(custId);
        }

        public async Task<Customer?> GetCustomerByNameAsync(string custName)
        {
            return await _customerRepository.GetCustomerByNameAsync(custName);
        }

        public async Task<IReadOnlyList<string>> GetCustomerNameSuggestionsAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return [];
            }

            return await _customerRepository.GetCustomerNameSuggestionsAsync(term.Trim(), max: 10);
        }

        public async Task<(bool Success, string? Error)> AddCustomerAsync(Customer customer)
        {
            if (!await _customerRepository.IsCustomerIdUniqueAsync(customer.CustId))
            {
                return (false, $"A customer with ID '{customer.CustId}' already exists.");
            }

            await _customerRepository.AddAsync(customer);
            await _customerRepository.SaveAsync();

            _logger.LogInformation("Created customer {CustId} ({Name})", customer.CustId, customer.CustName);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateCustomerAsync(Customer customer)
        {
            var existing = await _customerRepository.GetCustomerByIdAsync(customer.CustId);
            if (existing is null)
            {
                return (false, "Customer not found.");
            }

            _customerRepository.Update(customer);
            await _customerRepository.SaveAsync();

            _logger.LogInformation("Updated customer {CustId}", customer.CustId);
            return (true, null);
        }

        public async Task<bool> DeleteCustomerAsync(string custId)
        {
            var customer = await _customerRepository.GetCustomerByIdAsync(custId);
            if (customer is null)
            {
                return false;
            }

            // Re-attach a stub entity to delete without an extra SELECT round-trip.
            var stub = new Customer { CustId = custId };
            _customerRepository.Delete(stub);
            await _customerRepository.SaveAsync();

            _logger.LogInformation("Deleted customer {CustId}", custId);
            return true;
        }

        public async Task<IReadOnlyList<CustomerAddressDistributionDto>> GetCustomerAddressDistributionAsync()
        {
            return await _customerRepository.GetAddressDistributionAsync();
        }
    }
}