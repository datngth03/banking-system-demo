namespace BankingSystem.Infrastructure.Services;

using BankingSystem.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Service for encrypting/decrypting sensitive data using AES
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
        var keyString = configuration["Encryption:Key"] ?? "DefaultEncryptionKey12345678901234567890";
        _key = Encoding.UTF8.GetBytes(keyString.PadRight(32).Substring(0, 32));

        // Generate IV (16 bytes for AES)
        var ivString = configuration["Encryption:IV"] ?? "DefaultIV12345678";
        _iv = Encoding.UTF8.GetBytes(ivString.PadRight(16).Substring(0, 16));
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

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
            return Convert.ToBase64String(encrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting data");
            throw new InvalidOperationException("Failed to encrypt data", ex);
        }
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(Convert.FromBase64String(encryptedText));
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting data");
            throw new InvalidOperationException("Failed to decrypt data", ex);
        }
    }

    public string Mask(string value, int visibleChars = 4)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= visibleChars)
            return new string('*', value.Length);

        var maskedLength = value.Length - visibleChars;
        var masked = new string('*', maskedLength);
        var visible = value.Substring(value.Length - visibleChars);

        return masked + visible;
    }
}