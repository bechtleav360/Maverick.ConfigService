using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Bechtle.A365.ConfigService
{
    public static class X509CertificateUtility
    {
        /// <summary>
        /// Loads a certificate from a given certificate store
        /// </summary>
        /// <param name="storeName">Name of the certificate store</param>
        /// <param name="storeLocation">Location of the certificate store</param>
        /// <param name="thumbprint">Thumbprint of the certificate to load</param>
        /// <returns>An <see cref="X509Certificate2"/> certificate</returns>
        public static X509Certificate2 LoadCertificate(StoreName storeName, StoreLocation storeLocation, string thumbprint)
        {
            // The following code gets the cert from the keystore
            X509Store store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.ReadOnly);

            X509Certificate2Collection certCollection =
                    store.Certificates.Find(X509FindType.FindByThumbprint,
                    thumbprint, false);

            X509Certificate2Enumerator enumerator = certCollection.GetEnumerator();

            X509Certificate2 cert = null;

            while (enumerator.MoveNext())
            {
                cert = enumerator.Current;
            }

            return cert;
        }

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


        public static RSACryptoServiceProvider PrivateKeyFromPemFile(string privKey)
        {
            using (TextReader privateKeyTextReader = new StringReader(privKey))
            {
                AsymmetricCipherKeyPair readKeyPair = (AsymmetricCipherKeyPair)new PemReader(privateKeyTextReader).ReadObject();




                RsaPrivateCrtKeyParameters privateKeyParams = ((RsaPrivateCrtKeyParameters)readKeyPair.Private);
                RSACryptoServiceProvider cryptoServiceProvider = new RSACryptoServiceProvider();
                RSAParameters parms = new RSAParameters();

                parms.Modulus = privateKeyParams.Modulus.ToByteArrayUnsigned();
                parms.P = privateKeyParams.P.ToByteArrayUnsigned();
                parms.Q = privateKeyParams.Q.ToByteArrayUnsigned();
                parms.DP = privateKeyParams.DP.ToByteArrayUnsigned();
                parms.DQ = privateKeyParams.DQ.ToByteArrayUnsigned();
                parms.InverseQ = privateKeyParams.QInv.ToByteArrayUnsigned();
                parms.D = privateKeyParams.Exponent.ToByteArrayUnsigned();
                parms.Exponent = privateKeyParams.PublicExponent.ToByteArrayUnsigned();

                cryptoServiceProvider.ImportParameters(parms);

                return cryptoServiceProvider;
            }
        }


        private static string LoadPublicKey(byte[] data)
        {
            string pem = Encoding.ASCII.GetString(data);
            string header = "-----BEGIN CERTIFICATE-----";
            string footer = "-----END CERTIFICATE-----";
            int start = pem.IndexOf(header) + header.Length;
            int end = pem.IndexOf(footer, start);
            string base64 = pem.Substring(start, (end - start));
            return base64;
        }


        private static string LoadPrivateKey(byte[] data)
        {
            string pem = Encoding.ASCII.GetString(data);
            string header = "-----BEGIN RSA PRIVATE KEY-----";
            string footer = "-----END RSA PRIVATE KEY-----";
            int start = pem.IndexOf(header);
            int end = pem.IndexOf(footer, start) + footer.Length;
            string base64 = pem.Substring(start, (end - start));
            return base64;
        }


    }
}
