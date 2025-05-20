using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
namespace CMAPI.Services.Encryption;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _aesKey;
    private readonly byte[] _hmacKey;

    public AesEncryptionService(IConfiguration config)
    {
        // Expect a single Base64 string made by concatenating 64 bytes: 32 bytes AES key + 32 bytes HMAC key
        var combinedKey = Convert.FromBase64String(config["Encryption:Key"]);
        if (combinedKey.Length != 64)
            throw new ArgumentException("Encryption:Key must be 64 bytes in Base64");

        _aesKey  = combinedKey.Take(32).ToArray();
        _hmacKey = combinedKey.Skip(32).Take(32).ToArray();
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.KeySize   = 256;
        aes.BlockSize = 128;
        aes.Mode      = CipherMode.CBC;
        aes.Padding   = PaddingMode.PKCS7;
        aes.Key       = _aesKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Assemble: IV (16) || ciphertext
        var ivAndCipher = aes.IV.Concat(cipherBytes).ToArray();

        // Compute HMAC over (IV || ciphertext)
        using var hmac = new HMACSHA256(_hmacKey);
        var tag = hmac.ComputeHash(ivAndCipher);

        // Final payload: IV||cipher||tag
        var final = ivAndCipher.Concat(tag).ToArray();
        return Convert.ToBase64String(final);
    }

    public string Decrypt(string cipherText)
    {
        var allBytes = Convert.FromBase64String(cipherText);

        // extract: IV(16) | ciphertext | tag(32)
        var iv       = allBytes.Take(16).ToArray();
        var tagOffset = allBytes.Length - 32;
        var cipher   = allBytes.Skip(16).Take(tagOffset - 16).ToArray();
        var tag      = allBytes.Skip(tagOffset).Take(32).ToArray();

        // verify HMAC
        using (var hmac = new HMACSHA256(_hmacKey))
        {
            var computedTag = hmac.ComputeHash(allBytes.Take(tagOffset).ToArray());
            if (!computedTag.SequenceEqual(tag))
                throw new CryptographicException("Invalid HMAC");
        }

        // decrypt
        using var aes = Aes.Create();
        aes.KeySize   = 256;
        aes.BlockSize = 128;
        aes.Mode      = CipherMode.CBC;
        aes.Padding   = PaddingMode.PKCS7;
        aes.Key       = _aesKey;
        aes.IV        = iv;

        using var decryptor = aes.CreateDecryptor();
        var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return System.Text.Encoding.UTF8.GetString(plain);
    }
}