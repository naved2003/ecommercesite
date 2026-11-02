using ecommercesite.Data;
using ecommercesite.Models;
using Microsoft.EntityFrameworkCore;

namespace ecommercesite.Repositories
{
    public class PaymentRepository(ApplicationDbContext context) : GenericRepository<Payment>(context), IPaymentRepository
    {
        public async Task<Payment?> GetLatestPaymentForOrderAsync(int orderId)
        {
            return await _context.Payments
                                 .AsNoTracking()
                                 .Where(p => p.OrderId == orderId)
                                 .OrderByDescending(p => p.CreatedAt)
                                 .FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<Payment>> GetPaymentsForOrderAsync(int orderId)
        {
            return await _context.Payments
                                 .AsNoTracking()
                                 .Where(p => p.OrderId == orderId)
                                 .OrderByDescending(p => p.CreatedAt)
                                 .ToListAsync();
        }

        public async Task<bool> HasSuccessfulPaymentAsync(int orderId)
        {
            return await _context.Payments
                                 .AsNoTracking()
                                 .AnyAsync(p => p.OrderId == orderId && p.Status == "Paid");
        }
    }
}