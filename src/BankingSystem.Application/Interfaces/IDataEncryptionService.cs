namespace BankingSystem.Application.Interfaces;

/// <summary>
/// Interface for data encryption/decryption operations
/// </summary>
public interface IDataEncryptionService
{
    /// <summary>
    /// Encrypts sensitive data
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts sensitive data
    /// </summary>
    string Decrypt(string encryptedText);

    /// <summary>
    /// Masks sensitive data for logging (shows only last 4 characters)
    /// </summary>
    string Mask(string value, int visibleChars = 4);
}