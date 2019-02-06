﻿using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Bechtle.A365.ConfigService.Authentication.Certificates;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.Logging.NLog.Adapter;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;

namespace Bechtle.A365.ConfigService
{
    /// <summary>
    /// </summary>
    public class Program
    {
        /// <summary>
        ///     Build the WebHost that runs this application
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) => CreateWebHostBuilder(args).Build().Run();

        /// <summary>
        ///     Create and Configure a WebHostBuilder
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
            => WebHost.CreateDefaultBuilder(args)
                      .UseStartup<Startup>()
                      .ConfigureLogging((context, builder) =>
                      {
                          // set the global NLog configuration
                          using (var stringReader = new StringReader(context.Configuration
                                                                            .Get<ConfigServiceConfiguration>()
                                                                            .LoggingConfiguration))
                          using (var xmlReader = XmlReader.Create(stringReader))
                              LogManager.Configuration = new XmlLoggingConfiguration(xmlReader, null);

                          builder.ClearProviders()
                                 .AddProvider(new A365NLogProvider());
                      })
                      .UseKestrel(options =>
                      {
                          var logger = options.ApplicationServices
                                              .GetService<ILogger<Program>>();

                          var settings = options.ApplicationServices
                                                .GetService<ConfigServiceConfiguration>()
                                                ?.Authentication
                                                ?.Kestrel;

                          if (settings is null)
                          {
                              logger.LogWarning($"{nameof(AuthenticationConfiguration)} is null, can't configure kestrel");
                              return;
                          }

                          if (!settings.Enabled)
                          {
                              logger.LogWarning("skipping configuration of kestrel");
                              return;
                          }

                          var connectionOptions = new HttpsConnectionAdapterOptions
                          {
                              ServerCertificate = settings.Password is null
                                                      ? new X509Certificate2(settings.Certificate)
                                                      : new X509Certificate2(settings.Certificate, settings.Password),
                              ClientCertificateMode = ClientCertificateMode.RequireCertificate,
                              ClientCertificateValidation = CertificateValidator.DisableChannelValidation
                          };

                          logger.LogInformation($"loaded certificate: {connectionOptions.ServerCertificate}");

                          if (settings.IpAddress == "localhost")
                          {
                              logger.LogInformation($"binding to: localhost:{settings.Port}");
                              options.ListenLocalhost(settings.Port, listenOptions => { listenOptions.UseHttps(connectionOptions); });
                          }
                          else
                          {
                              var ip = IPAddress.Parse(settings.IpAddress);
                              logger.LogInformation($"binding to: {ip}:{settings.Port}");
                              options.Listen(ip, settings.Port, listenOptions => { listenOptions.UseHttps(connectionOptions); });
                          }
                      });
    }
}