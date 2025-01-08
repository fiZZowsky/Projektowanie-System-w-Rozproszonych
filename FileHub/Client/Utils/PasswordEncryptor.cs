using Common;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Client.Utils;

public static class PasswordEncryptor
{
    public static string EncryptPassword(string password)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(AppSettings.EncryptionKey);
            aes.IV = aes.Key.Take(16).ToArray();

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var writer = new StreamWriter(cs))
                {
                    writer.Write(password);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
}
