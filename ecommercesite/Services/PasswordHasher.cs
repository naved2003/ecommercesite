using System.Security.Cryptography;
using System.Text;

namespace ecommercesite.Services
{
    /// <summary>
    /// PBKDF2-SHA256 password hasher. 100,000 iterations and a 16-byte salt
    /// is a reasonable starting point for an educational app — bump the
    /// iteration count for production. The stored string is
    /// "{iterations}.{saltBase64}.{hashBase64}" so we can later raise the
    /// iteration count and re-hash on next login.
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltBytes = 16;
        private const int HashBytes = 32;
        private const int Iterations = 100_000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public string Hash(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password must not be empty.", nameof(password));
            }

            var salt = RandomNumberGenerator.GetBytes(SaltBytes);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), salt, Iterations, Algorithm, HashBytes);

            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(stored))
            {
                return false;
            }

            var parts = stored.Split('.');
            if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
            {
                return false;
            }

            byte[] salt, expected;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expected = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actual = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), salt, iterations, Algorithm, expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}
