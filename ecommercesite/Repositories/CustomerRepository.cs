using ecommercesite.Data;
using ecommercesite.Models;
using ecommercesite.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ecommercesite.Repositories
{
    public class CustomerRepository(ApplicationDbContext context) : GenericRepository<Customer>(context), ICustomerRepository
    {
        public async Task<Customer?> GetCustomerByIdAsync(string custId)
        {
            return await _context.Customers
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(c => c.CustId == custId);
        }

        public async Task<Customer?> GetCustomerByNameAsync(string custName)
        {
            return await _context.Customers
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(c => c.CustName == custName);
        }

        public async Task<IReadOnlyList<string>> GetCustomerNameSuggestionsAsync(string term, int max = 10)
        {
            return await _context.Customers
                                 .AsNoTracking()
                                 .Where(c => c.CustName.Contains(term))
                                 .Select(c => c.CustName)
                                 .Distinct()
                                 .OrderBy(n => n)
                                 .Take(max)
                                 .ToListAsync();
        }

        public async Task<bool> IsCustomerIdUniqueAsync(string custId)
        {
            return !await _context.Customers.AnyAsync(c => c.CustId == custId);
        }

        public async Task<IReadOnlyList<CustomerAddressDistributionDto>> GetAddressDistributionAsync()
        {
            // Group in SQL — coalesce blank / null addresses to "(no address)"
            // so they still show up as a slice on the pie chart.
            var rows = await _context.Customers
                .AsNoTracking()
                .GroupBy(c => c.CustAddress == null || c.CustAddress.Trim() == ""
                              ? "(no address)"
                              : c.CustAddress.Trim())
                .Select(g => new CustomerAddressDistributionDto
                {
                    Address = g.Key,
                    CustomerCount = g.Count()
                })
                .OrderByDescending(r => r.CustomerCount)
                .ThenBy(r => r.Address)
                .ToListAsync();

            return rows;
        }
    }
}