using System;
using System.IO;
using System.Security.Cryptography;

namespace XPlayer.Desktop.Common;

public static class SecurityUtil
{
    private static readonly byte[] Key =
    [
        0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF, 
        0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF,
        0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF,
        0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF
    ];
    
    // 16 bytes for AES block size (IV)
    private static readonly byte[] Iv =
    [
        0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF, 
        0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF
    ];

    public static string EncryptString(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;

        try
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = Iv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var msEncrypt = new MemoryStream();
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            return Convert.ToBase64String(msEncrypt.ToArray());
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string DecryptString(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText)) return string.Empty;

        try
        {
            var cipherText = Convert.FromBase64String(encryptedText);

            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = Iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using var msDecrypt = new MemoryStream(cipherText);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            return srDecrypt.ReadToEnd();
        }
        catch
        {
            return string.Empty;
        }
    }
}
