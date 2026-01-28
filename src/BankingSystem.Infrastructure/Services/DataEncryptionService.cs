namespace BankingSystem.Infrastructure.Services;

using BankingSystem.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Service for encrypting/decrypting sensitive data using AES-256
/// </summary>
public class DataEncryptionService : IDataEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;
    private readonly ILogger<DataEncryptionService> _logger;

    public DataEncryptionService(IConfiguration configuration, ILogger<DataEncryptionService> logger)
    {
        _logger = logger;

        // Get encryption key from configuration (should be 32 bytes for AES-256)
        var keyString = configuration["Encryption:Key"] 
            ?? throw new InvalidOperationException("Encryption:Key not configured");
        
        if (keyString.Length < 32)
        {
            _logger.LogWarning("Encryption key is shorter than 32 bytes, padding with default values");
        }

        _key = Encoding.UTF8.GetBytes(keyString.PadRight(32).Substring(0, 32));

        // Generate IV (16 bytes for AES)
        var ivString = configuration["Encryption:IV"] 
            ?? throw new InvalidOperationException("Encryption:IV not configured");
        
        _iv = Encoding.UTF8.GetBytes(ivString.PadRight(16).Substring(0, 16));
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using var sw = new StreamWriter(cs);

            sw.Write(plainText);
            sw.Close();

            var encrypted = ms.ToArray();
            _logger.LogDebug("Successfully encrypted data");
            return Convert.ToBase64String(encrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting data");
            throw;
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return cipherText;
        }

        try
        {
            var buffer = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(buffer);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            var decrypted = sr.ReadToEnd();
            _logger.LogDebug("Successfully decrypted data");
            return decrypted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting data");
            throw;
        }
    }

    public async Task<string> EncryptAsync(string plainText)
    {
        return await Task.Run(() => Encrypt(plainText));
    }

    public async Task<string> DecryptAsync(string cipherText)
    {
        return await Task.Run(() => Decrypt(cipherText));
    }

    public string Mask(string value, int visibleChars = 4)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= visibleChars)
        {
            return new string('*', value?.Length ?? 0);
        }

        var masked = new string('*', value.Length - visibleChars) + value.Substring(value.Length - visibleChars);
        return masked;
    }
}