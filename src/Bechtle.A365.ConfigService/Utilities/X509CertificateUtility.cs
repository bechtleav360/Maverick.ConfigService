using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;

namespace Bechtle.A365.ConfigService.Utilities
{
    /// <summary>
    ///     Utility to load Certificates from Stores or Files
    /// </summary>
    public static class X509CertificateUtility
    {
        /// <summary>
        ///     Loads a certificate from a given certificate store
        /// </summary>
        /// <param name="storeName">Name of the certificate store</param>
        /// <param name="storeLocation">Location of the certificate store</param>
        /// <param name="thumbprint">Thumbprint of the certificate to load</param>
        /// <returns>An <see cref="X509Certificate2" /> certificate</returns>
        public static X509Certificate2 LoadCertificate(StoreName storeName, StoreLocation storeLocation, string thumbprint)
        {
            // The following code gets the cert from the keystore
            var store = new X509Store(storeName, storeLocation);

            store.Open(OpenFlags.ReadOnly);

            var enumerator = store.Certificates
                                  .Find(X509FindType.FindByThumbprint, thumbprint, false)
                                  .GetEnumerator();

            X509Certificate2 cert = null;

            while (enumerator.MoveNext())
                cert = enumerator.Current;

            return cert;
        }

        /// <summary>
        ///     Loads a certificate from a given File
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static X509Certificate2 LoadFromCrt(string filepath)
        {
            var bytes = File.ReadAllBytes(filepath);
            var publicKey = LoadPublicKey(bytes);
            var cert = new X509Certificate2(Convert.FromBase64String(publicKey));
            var key = LoadPrivateKey(bytes);
            var rsa = PrivateKeyFromPemFile(key);
            cert = cert.CopyWithPrivateKey(rsa);

            return new X509Certificate2(cert.Export(X509ContentType.Pfx));
        }

        private static string LoadPrivateKey(byte[] data)
        {
            const string header = "-----BEGIN RSA PRIVATE KEY-----";
            const string footer = "-----END RSA PRIVATE KEY-----";

            var pem = Encoding.ASCII.GetString(data);
            var start = pem.IndexOf(header, StringComparison.OrdinalIgnoreCase);
            var end = pem.IndexOf(footer, start, StringComparison.OrdinalIgnoreCase) + footer.Length;
            var base64 = pem.Substring(start, end - start);

            return base64;
        }

        private static string LoadPublicKey(byte[] data)
        {
            const string header = "-----BEGIN CERTIFICATE-----";
            const string footer = "-----END CERTIFICATE-----";

            var pem = Encoding.ASCII.GetString(data);
            var start = pem.IndexOf(header, StringComparison.OrdinalIgnoreCase) + header.Length;
            var end = pem.IndexOf(footer, start, StringComparison.OrdinalIgnoreCase);
            var base64 = pem.Substring(start, end - start);

            return base64;
        }

        private static RSACryptoServiceProvider PrivateKeyFromPemFile(string privateKey)
        {
            using (TextReader privateKeyTextReader = new StringReader(privateKey))
            {
                var readKeyPair = (AsymmetricCipherKeyPair) new PemReader(privateKeyTextReader).ReadObject();
                var privateKeyParams = (RsaPrivateCrtKeyParameters) readKeyPair.Private;
                var cryptoServiceProvider = new RSACryptoServiceProvider();
                cryptoServiceProvider.ImportParameters(new RSAParameters
                {
                    Modulus = privateKeyParams.Modulus.ToByteArrayUnsigned(),
                    P = privateKeyParams.P.ToByteArrayUnsigned(),
                    Q = privateKeyParams.Q.ToByteArrayUnsigned(),
                    DP = privateKeyParams.DP.ToByteArrayUnsigned(),
                    DQ = privateKeyParams.DQ.ToByteArrayUnsigned(),
                    InverseQ = privateKeyParams.QInv.ToByteArrayUnsigned(),
                    D = privateKeyParams.Exponent.ToByteArrayUnsigned(),
                    Exponent = privateKeyParams.PublicExponent.ToByteArrayUnsigned()
                });

                return cryptoServiceProvider;
            }
        }
    }
}