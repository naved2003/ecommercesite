using ecommercesite.Data;
using ecommercesite.Models;
using Microsoft.EntityFrameworkCore;

namespace ecommercesite.Repositories
{
    public class RegisterRepository(ApplicationDbContext context) : GenericRepository<Register>(context), IRegisterRepository
    {
        public async Task<Register?> GetRegisterByIdAsync(string userId)
        {
            return await _context.Registers
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(r => r.UserId == userId);
        }

        public async Task<Register?> GetRegisterByNameAsync(string firstName)
        {
            return await _context.Registers
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(r => r.FirstName == firstName);
        }

        public async Task<Register?> GetRegisterByEmailAsync(string email)
        {
            return await _context.Registers
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(r => r.Email == email);
        }

        public async Task<bool> IsEmailUniqueAsync(string email, string? excludeId = null)
        {
            // When excludeId is supplied (editing an existing account), skip
            // that account's own row so a no-op email change doesn't fail.
            return !await _context.Registers
                                  .AnyAsync(r => r.Email == email
                                                 && (excludeId == null || r.UserId != excludeId));
        }

        public async Task<IReadOnlyList<Register>> GetByRoleAsync(string role)
        {
            return await _context.Registers
                                 .AsNoTracking()
                                 .Where(r => r.Role == role)
                                 .OrderBy(r => r.FirstName)
                                 .ThenBy(r => r.LastName)
                                 .ToListAsync();
        }
    }
}
