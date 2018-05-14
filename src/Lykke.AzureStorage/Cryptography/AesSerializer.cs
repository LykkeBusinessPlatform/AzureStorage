using System;
using System.IO;
using System.Security.Cryptography;
using JetBrains.Annotations;

namespace Lykke.AzureStorage.Cryptography
{
    /// <summary>
    /// aes256 pcks7 + IV
    /// </summary>
    public class AesSerializer : ICryptographicSerializer
    {
        private const string Prefix = "Enc|\n";
        private const char Separator = '\n';
        private byte[] _key;

        public AesSerializer([NotNull] string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            SetKey(Convert.FromBase64String(key));
        }

        private void SetKey(byte[] key)
        {
            if (key.Length != 32)
            {
                throw new ArgumentException($"Incorrect key size {key.Length}. Expected: 32", nameof(key));
            }

            _key = key;
        }

        public string Serialize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Data cannot be empty", nameof(value));
            }

            var buf = Encrypt(value, _key, out var iv);

            var ivText = Convert.ToBase64String(iv);
            var bufText = Convert.ToBase64String(buf);

            return $"{Prefix}{ivText}{Separator}{bufText}";
        }

        public string Deserialize(string value)
        {
            if (!IsEncrypted(value))
            {
                throw new ArgumentException("Data is not encrypted or not supported format", nameof(value));
            }

            var prm = value.Substring(Prefix.Length).Split(Separator);
            var iv = Convert.FromBase64String(prm[0]);
            var cipherText = Convert.FromBase64String(prm[1]);

            return Decrypt(cipherText, _key, iv);
        }

        public bool IsEncrypted(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Data cannot be empty", nameof(value));
            }

            return value.StartsWith(Prefix);
        }

        private static byte[] Encrypt(string plainText, byte[] key, out byte[] iv)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));

            byte[] encrypted;

            using (var algo = Aes.Create())
            {
                algo.KeySize = 256;
                algo.Key = key;
                algo.GenerateIV();
                iv = algo.IV;

                using (var encryptor = algo.CreateEncryptor(algo.Key, algo.IV))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (var streamWriter = new StreamWriter(cryptoStream))
                            {
                                streamWriter.Write(plainText);
                            }

                            encrypted = memoryStream.ToArray();
                        }
                    }
                }
            }

            return encrypted;
        }

        private static string Decrypt(byte[] cipherText, byte[] key, byte[] iv)
        {
            if (cipherText == null || cipherText.Length == 0)
                throw new ArgumentNullException(nameof(cipherText));
            if (iv == null || iv.Length == 0)
                throw new ArgumentNullException(nameof(iv));

            string plaintext;

            using (var algo = Aes.Create())
            {
                algo.KeySize = 256;
                algo.Key = key;
                algo.IV = iv;

                using (var decryptor = algo.CreateDecryptor(algo.Key, algo.IV))
                {
                    using (var memoryStream = new MemoryStream(cipherText))
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (var streamReader = new StreamReader(cryptoStream))
                            {
                                plaintext = streamReader.ReadToEnd();
                            }
                        }
                    }
                }
            }

            return plaintext;
        }
    }
}
