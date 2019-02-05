// Copyright (c) Barry Dorrans. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Bechtle.A365.ConfigService.Authentication.Certificates
{
    /// <summary>
    ///     Default values related to certificate authentication middleware
    /// </summary>
    public static class CertificateAuthenticationDefaults
    {
        /// <summary>
        ///      The default value used for CertificateAuthenticationOptions.AuthenticationScheme
        /// </summary>
        public const string AuthenticationScheme = "Certificate";
    }
}