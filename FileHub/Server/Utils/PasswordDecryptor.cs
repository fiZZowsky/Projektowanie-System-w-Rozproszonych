using Common;
using System.Security.Cryptography;
using System.Text;

namespace Server.Utils;

public static class PasswordDecryptor
{
    public static string DecryptPassword(string encryptedPassword)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(AppSettings.EncryptionKey);
            aes.IV = aes.Key.Take(16).ToArray();

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream(Convert.FromBase64String(encryptedPassword)))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var reader = new StreamReader(cs))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
