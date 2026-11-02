using ecommercesite.Data;
using ecommercesite.Models;
using Microsoft.EntityFrameworkCore;

namespace ecommercesite.Repositories
{
    public class LoginRepository(ApplicationDbContext context) : ILoginRepository
    {
        private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<Register?> GetUserByEmailAsync(string email)
        {
            // Tracked on purpose — login may update LastLogin etc. later.
            return await _context.Registers
                                 .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        }

        public async Task<bool> IsActiveAsync(string email)
        {
            return await _context.Registers
                                 .AsNoTracking()
                                 .AnyAsync(u => u.Email == email && u.IsActive);
        }
    }
}