using System.Security.Cryptography;
using System.Text;

namespace Pridr_IntergrationTest.Helpers;

public class EncryptionHelper
{
    private static byte[] GetKey()
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(Encoding.UTF8.GetBytes("YourSecurePassword1234"));
        }
    }

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        var fullCipher = Convert.FromBase64String(cipherText);

        using (Aes aes = Aes.Create())
        {
            aes.Key = GetKey();

            // Haal de IV uit de eerste 16 bytes van de ciphertext
            var iv = new byte[16];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);

            // Haal de echte ciphertext eruit
            var cipherBytes = new byte[fullCipher.Length - iv.Length];
            Array.Copy(fullCipher, iv.Length, cipherBytes, 0, cipherBytes.Length);

            using (var ms = new MemoryStream(cipherBytes))
            using (var decryptor = aes.CreateDecryptor(aes.Key, iv))
            using (var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var reader = new StreamReader(cryptoStream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}