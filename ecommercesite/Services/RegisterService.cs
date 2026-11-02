using ecommercesite.Models;
using ecommercesite.Repositories;
using ecommercesite.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ecommercesite.Services
{
    /// <summary>
    /// Handles user registration. All NotImplementedException stubs from the
    /// previous version have been implemented.
    /// </summary>
    public class RegisterService(
        IRegisterRepository registerRepository,
        IPasswordHasher passwordHasher,
        ILogger<RegisterService> logger) : IRegisterService
    {
        private readonly IRegisterRepository _registerRepository = registerRepository;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly ILogger<RegisterService> _logger = logger;

        public async Task<IEnumerable<Register>> GetAllRegistersAsync()
        {
            return await _registerRepository.GetAllAsync(
                orderBy: q => q.OrderBy(r => r.FirstName).ThenBy(r => r.LastName));
        }

        public async Task<Register?> GetRegisterByIdAsync(string userId)
        {
            return await _registerRepository.GetRegisterByIdAsync(userId);
        }

        public async Task<Register?> GetRegisterByNameAsync(string firstName)
        {
            return await _registerRepository.GetRegisterByNameAsync(firstName);
        }

        public async Task<Register?> GetRegisterByEmailAsync(string email)
        {
            return await _registerRepository.GetRegisterByEmailAsync(email);
        }

        public async Task<(bool Success, string? Error)> AddRegisterAsync(RegisterViewModel model)
        {
            if (!await _registerRepository.IsEmailUniqueAsync(model.Email))
            {
                return (false, "An account with this email already exists.");
            }

            // The very first user to register is auto-promoted to Admin so
            // the system has an administrator out of the box. Everyone else
            // defaults to Customer.
            var userCount = await _registerRepository.CountAsync();
            var role = userCount == 0 ? "Admin" : "Customer";

            var entity = new Register
            {
                UserId = Guid.NewGuid().ToString("N"),
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Email = model.Email.Trim().ToLowerInvariant(),
                Password = _passwordHasher.Hash(model.Password),
                PhoneNumber = model.PhoneNumber.Trim(),
                IsActive = true,
                Role = role,
                CreatedAt = DateTime.UtcNow
            };

            await _registerRepository.AddAsync(entity);
            await _registerRepository.SaveAsync();

            _logger.LogInformation("Registered new {Role} user {Email} ({UserId})",
                entity.Role, entity.Email, entity.UserId);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateRegisterAsync(Register reg)
        {
            var existing = await _registerRepository.GetRegisterByIdAsync(reg.UserId);
            if (existing is null)
            {
                return (false, "User not found.");
            }

            reg.UpdatedAt = DateTime.UtcNow;
            _registerRepository.Update(reg);
            await _registerRepository.SaveAsync();

            _logger.LogInformation("Updated user {UserId}", reg.UserId);
            return (true, null);
        }

        public async Task<bool> DeleteRegisterAsync(string userId)
        {
            var stub = new Register { UserId = userId };
            _registerRepository.Delete(stub);
            await _registerRepository.SaveAsync();

            _logger.LogInformation("Deleted user {UserId}", userId);
            return true;
        }

        public async Task<IReadOnlyList<Register>> GetCustomerAccountsAsync()
        {
            return await _registerRepository.GetByRoleAsync("Customer");
        }

        public async Task<(bool Success, string? Error)> DeleteCustomerAccountAsync(string userId)
        {
            var account = await _registerRepository.GetRegisterByIdAsync(userId);
            if (account is null)
            {
                return (false, "Account not found.");
            }

            // Safety: never allow deleting Admin accounts from the customer
            // list page. Admin accounts can only be removed by direct DB
            // access (or a future admin-management page).
            if (account.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Admin accounts cannot be removed from the customer list.");
            }

            // Re-attach a stub for deletion (the GetRegisterByIdAsync call
            // above was AsNoTracking, so we need a tracked entity to delete).
            var stub = new Register { UserId = userId };
            _registerRepository.Delete(stub);
            await _registerRepository.SaveAsync();

            _logger.LogInformation("Admin deleted customer account {Email} ({UserId})",
                account.Email, userId);
            return (true, null);
        }

        // Allowed roles for admin-side account management.
        private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "Admin", "Customer"
        };

        public async Task<(bool Success, string? Error)> AddAccountAsync(
            string firstName, string lastName, string email,
            string password, string phoneNumber, string role)
        {
            if (!ValidRoles.Contains(role))
            {
                return (false, $"Invalid role '{role}'. Allowed: {string.Join(", ", ValidRoles)}.");
            }

            email = email.Trim().ToLowerInvariant();
            if (!await _registerRepository.IsEmailUniqueAsync(email))
            {
                return (false, "An account with this email already exists.");
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                return (false, "Password must be at least 6 characters.");
            }

            var entity = new Register
            {
                UserId = Guid.NewGuid().ToString("N"),
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                Email = email,
                Password = _passwordHasher.Hash(password),
                PhoneNumber = phoneNumber.Trim(),
                IsActive = true,
                Role = role,
                CreatedAt = DateTime.UtcNow
            };

            await _registerRepository.AddAsync(entity);
            await _registerRepository.SaveAsync();

            _logger.LogInformation("Admin created {Role} account {Email} ({UserId})",
                entity.Role, entity.Email, entity.UserId);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateAccountAsync(
            string userId, string firstName, string lastName,
            string email, string phoneNumber, string role,
            bool isActive, string? newPassword)
        {
            if (!ValidRoles.Contains(role))
            {
                return (false, $"Invalid role '{role}'. Allowed: {string.Join(", ", ValidRoles)}.");
            }

            // Fetch tracked entity — GetRegisterByIdAsync is AsNoTracking so
            // we need a tracked stub for the update mutations to stick.
            var existing = await _registerRepository.GetByIdAsync(userId);
            if (existing is null)
            {
                return (false, "Account not found.");
            }

            email = email.Trim().ToLowerInvariant();
            // Email uniqueness check (skip the row being edited).
            if (!await _registerRepository.IsEmailUniqueAsync(email, userId))
            {
                return (false, "Another account already uses this email.");
            }

            existing.FirstName = firstName.Trim();
            existing.LastName = lastName.Trim();
            existing.Email = email;
            existing.PhoneNumber = phoneNumber.Trim();
            existing.Role = role;
            existing.IsActive = isActive;
            existing.UpdatedAt = DateTime.UtcNow;

            // Optional password reset — only hash + set when a new password
            // was supplied. Empty/null = leave the existing password alone.
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                if (newPassword.Length < 6)
                {
                    return (false, "New password must be at least 6 characters.");
                }
                existing.Password = _passwordHasher.Hash(newPassword);
            }

            await _registerRepository.SaveAsync();

            _logger.LogInformation("Admin updated account {UserId} (role={Role})", userId, role);
            return (true, null);
        }
    }
}