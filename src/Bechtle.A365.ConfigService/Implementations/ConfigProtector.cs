using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Bechtle.A365.ConfigService.Interfaces;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <inheritdoc />
    public class ConfigProtector : IConfigProtector
    {
        /// <inheritdoc />
        public string DecryptWithPrivateKey(string data, X509Certificate2 cert)
        {
            using var rsa = cert.GetRSAPrivateKey();
            return DecryptInternal(data, rsa);
        }

        /// <inheritdoc />
        public string DecryptWithPublicKey(string data, X509Certificate2 cert)
        {
            using var rsa = cert.GetRSAPublicKey();
            return DecryptInternal(data, rsa);
        }

        /// <inheritdoc />
        public string EncryptWithPrivateKey(string data, X509Certificate2 cert)
        {
            using var rsa = cert.GetRSAPrivateKey();
            return EncryptInternal(data, rsa);
        }

        /// <inheritdoc />
        public string EncryptWithPublicKey(string data, X509Certificate2 cert)
        {
            using var rsa = cert.GetRSAPublicKey();
            return EncryptInternal(data, rsa);
        }

        /// <summary>
        ///     decrypt the given data using the same technique as <see cref="EncryptInternal" />
        /// </summary>
        /// <param name="data"></param>
        /// <param name="rsa"></param>
        /// <returns></returns>
        private string DecryptInternal(string data, RSA rsa)
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(data));

            var encrypted = JsonSerializer.Deserialize<EncryptionResult>(json);

            var decryptedMetadata = rsa.Decrypt(Convert.FromBase64String(encrypted.Metadata), RSAEncryptionPadding.OaepSHA512);
            var metadataJson = Encoding.UTF8.GetString(decryptedMetadata);
            var metadata = JsonSerializer.Deserialize<AesEncryptionMetadata>(metadataJson);

            using var aes = Aes.Create() ?? throw new NotSupportedException("Aes-Encryption not supported on current platform");
            aes.Key = Convert.FromBase64String(metadata.Key);
            aes.IV = Convert.FromBase64String(metadata.IV);

            using var memStream = new MemoryStream();
            using (var crypto = new CryptoStream(memStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
                var raw = Convert.FromBase64String(encrypted.Payload);
                crypto.Write(raw, 0, raw.Length);
            }

            return Encoding.UTF8.GetString(memStream.ToArray());
        }

        /// <summary>
        ///     encrypt the given data using
        /// </summary>
        /// <param name="data"></param>
        /// <param name="rsa"></param>
        /// <returns></returns>
        private string EncryptInternal(string data, RSA rsa)
        {
            using var aes = Aes.Create() ?? throw new NotSupportedException("Aes-Encryption not supported on current platform");

            byte[] encryptedPayload;

            using (var memStream = new MemoryStream())
            {
                using (var crypto = new CryptoStream(memStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    var raw = Encoding.UTF8.GetBytes(data);
                    crypto.Write(raw, 0, raw.Length);
                }

                encryptedPayload = memStream.ToArray();
            }

            var encryptedMetadata = rsa.Encrypt(
                JsonSerializer.SerializeToUtf8Bytes(
                    new AesEncryptionMetadata
                    {
                        Key = Convert.ToBase64String(aes.Key),
                        IV = Convert.ToBase64String(aes.IV)
                    }),
                RSAEncryptionPadding.OaepSHA512);

            return Convert.ToBase64String(
                JsonSerializer.SerializeToUtf8Bytes(
                    new EncryptionResult
                    {
                        Metadata = Convert.ToBase64String(encryptedMetadata),
                        Payload = Convert.ToBase64String(encryptedPayload)
                    }));
        }

        private class EncryptionResult
        {
            public string Metadata { get; set; }

            public string Payload { get; set; }
        }

        private class AesEncryptionMetadata
        {
            // ReSharper disable once InconsistentNaming
            // Name is not inconsistent, it's widely known to stand for...
            // InitializationVector
            public string IV { get; set; }

            public string Key { get; set; }
        }
    }
}