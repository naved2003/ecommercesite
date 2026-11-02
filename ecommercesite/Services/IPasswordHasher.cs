namespace ecommercesite.Services
{
    /// <summary>
    /// Password hashing utility. Uses PBKDF2-SHA256 with a per-user random
    /// salt. The stored format is "iterations.saltBase64.hashBase64".
    /// </summary>
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string stored);
    }
}