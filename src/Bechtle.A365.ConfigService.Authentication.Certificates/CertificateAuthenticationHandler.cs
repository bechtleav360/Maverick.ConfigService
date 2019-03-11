﻿// Copyright (c) Barry Dorrans. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Authentication.Certificates.Events;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bechtle.A365.ConfigService.Authentication.Certificates
{
    internal class CertificateAuthenticationHandler : AuthenticationHandler<CertificateAuthenticationOptions>
    {
        private static readonly Oid ClientCertificateOid = new Oid("1.3.6.1.5.5.7.3.2");

        public CertificateAuthenticationHandler(IOptionsMonitor<CertificateAuthenticationOptions> options,
                                                ILoggerFactory logger,
                                                UrlEncoder encoder,
                                                ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        /// <summary>
        ///     The handler calls methods on the events which give the application control at certain points where processing is occurring.
        ///     If it is not provided a default instance is supplied which does nothing when the methods are called.
        /// </summary>
        protected new CertificateAuthenticationEvents Events
        {
            get => (CertificateAuthenticationEvents) base.Events;
            set => base.Events = value;
        }

        /// <summary>
        ///     Creates a new instance of the events instance.
        /// </summary>
        /// <returns>A new instance of the events instance.</returns>
        protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new CertificateAuthenticationEvents());

        /// <inheritdoc />
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // You only get client certificates over HTTPS
            if (!Context.Request.IsHttps)
                return AuthenticateResult.NoResult();

            var clientCertificate = await Context.Connection.GetClientCertificateAsync();

            // This should never be the case, as cert authentication happens long before ASP.NET kicks in.
            if (clientCertificate == null)
            {
                Logger.LogDebug("No client certificate found.");
                return AuthenticateResult.NoResult();
            }

            // If we have a self signed cert, and they're not allowed, exit early and not bother with
            // any other validations.
            if (clientCertificate.IsSelfSigned() &&
                !Options.AllowedCertificateTypes.HasFlag(CertificateTypes.SelfSigned))
            {
                Logger.LogWarning("Self signed certificate rejected, subject was {0}", clientCertificate.Subject);
                return AuthenticateResult.Fail("Options do not allow self signed certificates.");
            }

            // If we have a chained cert, and they're not allowed, exit early and not bother with
            // any other validations.
            if (!clientCertificate.IsSelfSigned() &&
                !Options.AllowedCertificateTypes.HasFlag(CertificateTypes.Chained))
            {
                Logger.LogWarning("Chained certificate rejected, subject was {0}", clientCertificate.Subject);

                return AuthenticateResult.Fail("Options do not allow chained certificates.");
            }

            var chainPolicy = BuildChainPolicy(clientCertificate);

            try
            {
                var chain = new X509Chain
                {
                    ChainPolicy = chainPolicy
                };

                var certificateIsValid = chain.Build(clientCertificate);

                if (!certificateIsValid)
                {
                    using (Logger.BeginScope(clientCertificate.Sha256Thumbprint()))
                    {
                        Logger.LogWarning("Client certificate failed validation, subject was {0}", clientCertificate.Subject);
                        foreach (var validationFailure in chain.ChainStatus)
                            Logger.LogWarning("{0} {1}", validationFailure.Status, validationFailure.StatusInformation);
                    }

                    return AuthenticateResult.Fail("Client certificate failed validation.");
                }

                var validateCertificateContext = new ValidateCertificateContext(Context, Scheme, Options)
                {
                    ClientCertificate = clientCertificate
                };

                await Events.ValidateCertificate(validateCertificateContext);

                if (validateCertificateContext.Result != null &&
                    validateCertificateContext.Result.Succeeded)
                {
                    var ticket = new AuthenticationTicket(validateCertificateContext.Principal, Scheme.Name);
                    return AuthenticateResult.Success(ticket);
                }

                if (validateCertificateContext.Result?.Failure != null)
                    return AuthenticateResult.Fail(validateCertificateContext.Result.Failure);

                return AuthenticateResult.NoResult();
            }
            catch (Exception ex)
            {
                var authenticationFailedContext = new CertificateAuthenticationFailedContext(Context, Scheme, Options)
                {
                    Exception = ex
                };

                await Events.AuthenticationFailed(authenticationFailedContext);

                if (authenticationFailedContext.Result != null)
                    return authenticationFailedContext.Result;

                throw;
            }
        }

        /// <inheritdoc />
        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            // Certificate authentication takes place at the connection level. We can't prompt once we're in
            // user code, so the best thing to do is Forbid, not Challenge.
            Response.StatusCode = 403;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 403;
            return Task.CompletedTask;
        }

        private X509ChainPolicy BuildChainPolicy(X509Certificate2 certificate)
        {
            // Now build the chain validation options.
            X509RevocationFlag revocationFlag = Options.RevocationFlag;
            X509RevocationMode revocationMode = Options.RevocationMode;

            if (certificate.IsSelfSigned())
            {
                // Turn off chain validation, because we have a self signed certificate.
                revocationFlag = X509RevocationFlag.EntireChain;
                revocationMode = X509RevocationMode.NoCheck;
            }

            var chainPolicy = new X509ChainPolicy
            {
                RevocationFlag = revocationFlag,
                RevocationMode = revocationMode
            };

            if (Options.ValidateCertificateUse)
                chainPolicy.ApplicationPolicy.Add(ClientCertificateOid);

            if (certificate.IsSelfSigned())
            {
                chainPolicy.VerificationFlags |= X509VerificationFlags.AllowUnknownCertificateAuthority;
                chainPolicy.VerificationFlags |= X509VerificationFlags.IgnoreEndRevocationUnknown;
                chainPolicy.ExtraStore.Add(certificate);
            }

            if (!Options.ValidateValidityPeriod)
                chainPolicy.VerificationFlags |= X509VerificationFlags.IgnoreNotTimeValid;

            return chainPolicy;
        }
    }
}