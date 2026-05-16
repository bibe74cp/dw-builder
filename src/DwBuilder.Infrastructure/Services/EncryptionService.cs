using System.Security.Cryptography;
using System.Text;
using DwBuilder.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DwBuilder.Infrastructure.Services;

/// <summary>
/// Service for encrypting and decrypting sensitive data using AES-256-CBC.
/// The encryption key is read from configuration (Encryption:Key) and must be a base64-encoded 32-byte key.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    
    public EncryptionService(IConfiguration configuration)
    {
        var keyBase64 = configuration["Encryption:Key"];
        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            throw new InvalidOperationException(
                "Encryption key is not configured. Please set 'Encryption:Key' in appsettings.json with a base64-encoded 32-byte key.");
        }
        
        try
        {
            _key = Convert.FromBase64String(keyBase64);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "Encryption key is not a valid base64 string. Please ensure 'Encryption:Key' contains a valid base64-encoded 32-byte key.");
        }
        
        if (_key.Length != 32)
        {
            throw new InvalidOperationException(
                $"Encryption key must be exactly 32 bytes (256 bits). Current key length: {_key.Length} bytes.");
        }
    }
    
    /// <summary>
    /// Encrypts a plaintext string using AES-256-CBC with a random IV.
    /// </summary>
    /// <param name="plainText">The text to encrypt.</param>
    /// <returns>Base64-encoded string in format "IV:CipherText".</returns>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }
        
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();
        
        var iv = aes.IV;
        
        using var encryptor = aes.CreateEncryptor(aes.Key, iv);
        using var msEncrypt = new MemoryStream();
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }
        
        var cipherBytes = msEncrypt.ToArray();
        
        // Format: "IV:CipherText" both base64-encoded
        var ivBase64 = Convert.ToBase64String(iv);
        var cipherBase64 = Convert.ToBase64String(cipherBytes);
        
        return $"{ivBase64}:{cipherBase64}";
    }
    
    /// <summary>
    /// Decrypts a ciphertext string encrypted with AES-256-CBC.
    /// </summary>
    /// <param name="cipherText">Base64-encoded string in format "IV:CipherText".</param>
    /// <returns>The original plaintext.</returns>
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return cipherText;
        }
        
        var parts = cipherText.Split(':');
        if (parts.Length != 2)
        {
            throw new ArgumentException(
                "Invalid ciphertext format. Expected 'IV:CipherText' with both parts base64-encoded.");
        }
        
        byte[] iv;
        byte[] cipherBytes;
        
        try
        {
            iv = Convert.FromBase64String(parts[0]);
            cipherBytes = Convert.FromBase64String(parts[1]);
        }
        catch (FormatException)
        {
            throw new ArgumentException(
                "Invalid ciphertext format. IV or CipherText is not valid base64.");
        }
        
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var msDecrypt = new MemoryStream(cipherBytes);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        
        return srDecrypt.ReadToEnd();
    }
}
