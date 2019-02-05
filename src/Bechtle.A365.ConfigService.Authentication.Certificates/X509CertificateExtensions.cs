// Copyright (c) Barry Dorrans. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Bechtle.A365.ConfigService.Authentication.Certificates
{
    public static class X509Certificate2Extensions
    {
        public static bool IsSelfSigned(this X509Certificate2 certificate)
            => certificate.SubjectName.RawData.SequenceEqual(certificate.IssuerName.RawData);

        public static string SHA256Thumbprint(this X509Certificate2 certificate)
        {
            var certificateHash = SHA256.Create().ComputeHash(certificate.RawData);
            var hashString = string.Empty;

            foreach (var hashByte in certificateHash)
                hashString += hashByte.ToString("x2", CultureInfo.InvariantCulture);

            return hashString;
        }
    }
}