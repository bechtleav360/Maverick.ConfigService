// Copyright (c) Barry Dorrans. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Bechtle.A365.ConfigService.Authentication.Certificates.Events
{
    public class CertificateAuthenticationFailedContext : ResultContext<CertificateAuthenticationOptions>
    {
        public CertificateAuthenticationFailedContext(HttpContext context,
                                                      AuthenticationScheme scheme,
                                                      CertificateAuthenticationOptions options)
            : base(context, scheme, options)
        {
        }

        public Exception Exception { get; set; }
    }
}