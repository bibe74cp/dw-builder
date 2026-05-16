namespace DwBuilder.Core.Interfaces;

/// <summary>
/// Service interface for encrypting and decrypting sensitive data (e.g., passwords).
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a plaintext string using AES-256-CBC.
    /// </summary>
    /// <param name="plainText">The text to encrypt.</param>
    /// <returns>Base64-encoded string in format "IV:CipherText".</returns>
    string Encrypt(string plainText);
    
    /// <summary>
    /// Decrypts a ciphertext string encrypted with AES-256-CBC.
    /// </summary>
    /// <param name="cipherText">Base64-encoded string in format "IV:CipherText".</param>
    /// <returns>The original plaintext.</returns>
    string Decrypt(string cipherText);
}
