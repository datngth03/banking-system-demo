namespace BankingSystem.Infrastructure.Services;

using BankingSystem.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;

    public string HashPassword(string password)
    {
        // Generate a salt
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);

        // Hash the password with the salt
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256);

        var hash = pbkdf2.GetBytes(HashSize);

        // Combine salt and hash
        var hashBytes = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

        // Convert to base64 for storage
        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        try
        {
            // Extract the bytes
            var hashBytes = Convert.FromBase64String(hashedPassword);

            // Extract the salt
            var salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            // Hash the provided password with the extracted salt
            using var pbkdf2 = new Rfc2898DeriveBytes(
                providedPassword,
                salt,
                Iterations,
                HashAlgorithmName.SHA256);

            var hash = pbkdf2.GetBytes(HashSize);

            // Compare the hashes
            for (int i = 0; i < HashSize; i++)
            {
                if (hashBytes[i + SaltSize] != hash[i])
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
