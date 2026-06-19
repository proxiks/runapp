using System.IO;
using System.Security.Cryptography;

namespace RunApp.Desktop.Services;

public class LyfronCrypto
{
    private readonly byte[] _masterKey;

    public LyfronCrypto()
    {
        // Derive key from user's password + device ID
        // In production, use secure key storage
        _masterKey = DeriveKey();
    }

    public (byte[] encrypted, byte[] iv) Encrypt(string plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _masterKey;
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plaintext);
        }
        
        return (ms.ToArray(), aes.IV);
    }

    public string Decrypt(byte[] encrypted, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = _masterKey;
        aes.IV = iv;
        
        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(encrypted);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        
        return sr.ReadToEnd();
    }

    private byte[] DeriveKey()
    {
        // Use Argon2id via Lyfron C library
        // For now, derive from device + user
        var deviceId = Environment.MachineName;
        var userToken = Properties.Settings.Default.AuthToken;
        
        using var pbkdf2 = new Rfc2898DeriveBytes(
            userToken + deviceId,
            Encoding.UTF8.GetBytes("RunAppSalt2026"),
            100000,
            HashAlgorithmName.SHA256);
        
        return pbkdf2.GetBytes(32);
    }
}