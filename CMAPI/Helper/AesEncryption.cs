namespace CMAPI.Helper;
using System.Security.Cryptography;
using System.Text;
public class AesEncryption
{
    public static string Encrypt(string plainText, byte[] key)
    {
        if (key.Length != 32)
            throw new ArgumentException("Key must be 32 bytes for AES-256");

        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV(); 
        byte[] iv = aes.IV;

        using MemoryStream ms = new();
        ms.Write(iv, 0, iv.Length);
        using (CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        using (StreamWriter sw = new(cs, Encoding.UTF8))
        {
            sw.Write(plainText);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    public static string Decrypt(string cipherText, byte[] key)
    {
        byte[] fullCipher = Convert.FromBase64String(cipherText);
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
            
        byte[] iv = new byte[aes.BlockSize / 8];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using MemoryStream ms = new(fullCipher, iv.Length, fullCipher.Length - iv.Length);
        using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using StreamReader sr = new(cs, Encoding.UTF8);
        return sr.ReadToEnd();
    }
}