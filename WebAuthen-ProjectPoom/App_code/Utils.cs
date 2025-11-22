using System.Security.Cryptography;
using System.Text;

namespace WebAuthen.App_code;

public static class Utils
{
    public static async Task Base64ToFile(string base64, string path)
    {
        int commaIndex = base64.IndexOf(',');
        if (commaIndex >= 0) base64 = base64[(commaIndex + 1)..];

        byte[] fileBytes = Convert.FromBase64String(base64);

        await File.WriteAllBytesAsync(path, fileBytes);
    }

    public static string GenerateNumericOtp(int length = 6)
    {
        var digits = new List<string>(length);
        for (int i = 0; i < length; i++)
        {
            int d = RandomNumberGenerator.GetInt32(10);
            digits.Add(d.ToString());
        }
        return string.Concat(digits);
    }

    public static string DecryptRsa(string encrypted)
    {
        byte[] privateKeyBytes = Convert.FromBase64String(File.ReadAllText("private.key"));
        using RSA rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);

        byte[] encryptedBytes = Convert.FromBase64String(encrypted);
        byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
}