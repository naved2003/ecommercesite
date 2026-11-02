using ecommercesite.Models;
using ecommercesite.Repositories;

namespace ecommercesite.Services
{
    public class LoginService(
        ILoginRepository loginRepository,
        IPasswordHasher passwordHasher,
        ILogger<LoginService> logger) : ILoginService
    {
        private readonly ILoginRepository _loginRepository = loginRepository;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly ILogger<LoginService> _logger = logger;

        public async Task<Register?> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            var user = await _loginRepository.GetUserByEmailAsync(email.Trim().ToLowerInvariant());
            if (user is null)
            {
                _logger.LogWarning("Login attempt for unknown email {Email}", email);
                return null;
            }

            if (!_passwordHasher.Verify(password, user.Password))
            {
                _logger.LogWarning("Bad password for user {Email}", email);
                return null;
            }

            _logger.LogInformation("User {Email} logged in successfully", email);
            return user;
        }
    }
}