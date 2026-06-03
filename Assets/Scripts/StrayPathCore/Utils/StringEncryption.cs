using System;
using System.Security.Cryptography;
using System.Text;

namespace StrayPathCore.Utils
{
    /// <summary>
    /// 简单的对称字符串加密 —— 用于存档防 casual tampering。
    /// </summary>
    public static class StringEncryption
    {
        public static string Encrypt(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText)) return "";
            try
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(key.PadRight(16).Substring(0, 16));
                byte[] iv = new byte[16];
                using (var aes = Aes.Create())
                {
                    aes.Key = keyBytes;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using (var encryptor = aes.CreateEncryptor())
                    {
                        byte[] input = Encoding.UTF8.GetBytes(plainText);
                        byte[] output = encryptor.TransformFinalBlock(input, 0, input.Length);
                        return Convert.ToBase64String(output);
                    }
                }
            }
            catch
            {
                return plainText;
            }
        }

        public static string Decrypt(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText)) return "";
            try
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(key.PadRight(16).Substring(0, 16));
                byte[] iv = new byte[16];
                using (var aes = Aes.Create())
                {
                    aes.Key = keyBytes;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using (var decryptor = aes.CreateDecryptor())
                    {
                        byte[] input = Convert.FromBase64String(cipherText);
                        byte[] output = decryptor.TransformFinalBlock(input, 0, input.Length);
                        return Encoding.UTF8.GetString(output);
                    }
                }
            }
            catch
            {
                return cipherText;
            }
        }
    }
}
