using ecommercesite.Data;
using ecommercesite.Models;
using ecommercesite.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ecommercesite.Repositories
{
    public class DeliveryPersonRepository(ApplicationDbContext context) : GenericRepository<DeliveryPerson>(context), IDeliveryPersonRepository
    {
        public async Task<PagedResult<DeliveryPerson>> GetDeliveryPeopleAsync(string? search, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

            var term = search?.Trim();
            IQueryable<DeliveryPerson> query = _context.DeliveryPeople.AsNoTracking();

            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(d =>
                    d.Name.Contains(term) ||
                    d.Phone.Contains(term) ||
                    (d.Email != null && d.Email.Contains(term)) ||
                    (d.VehicleNumber != null && d.VehicleNumber.Contains(term)) ||
                    (d.AssignedArea != null && d.AssignedArea.Contains(term)));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(d => d.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<DeliveryPerson>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<DeliveryPerson?> GetDeliveryPersonByIdAsync(int id)
        {
            return await _context.DeliveryPeople
                                 .AsNoTracking()
                                 .Include(d => d.AssignedOrders)
                                    .ThenInclude(o => o.Customer)
                                 .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<DeliveryPerson?> GetDeliveryPersonByPhoneAsync(string phone)
        {
            return await _context.DeliveryPeople
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(d => d.Phone == phone);
        }

        public async Task<bool> IsPhoneUniqueAsync(string phone, int? excludeId = null)
        {
            return !await _context.DeliveryPeople
                                  .AnyAsync(d => d.Phone == phone && (excludeId == null || d.Id != excludeId.Value));
        }

        public async Task<IReadOnlyList<DeliveryPerson>> GetAvailableDeliveryPeopleAsync()
        {
            // "Available" delivery people = active + status == Available.
            return await _context.DeliveryPeople
                                 .AsNoTracking()
                                 .Where(d => d.IsActive && d.Status == "Available")
                                 .OrderBy(d => d.Name)
                                 .ToListAsync();
        }
    }
}