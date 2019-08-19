using System.Net;
using System.Security.Cryptography.X509Certificates;
using App.Metrics.AspNetCore;
using Bechtle.A365.ConfigService.Authentication.Certificates;
using Bechtle.A365.ConfigService.Common.Utilities;
using Bechtle.A365.ConfigService.Configuration;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Bechtle.A365.ConfigService
{
    /// <summary>
    ///     Main Entry-Point for the Application
    /// </summary>
    public class Program
    {
        /// <summary>
        ///     Build the WebHost that runs this application
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) => CreateWebHostBuilder(args).Build().Run();

        /// <summary>
        ///     Configure Kestrel to use Certificate-based Authentication
        /// </summary>
        /// <param name="options"></param>
        private static void ConfigureKestrelCertAuth(KestrelServerOptions options)
        {
            using (var scope = options.ApplicationServices.CreateScope())
            {
                var logger = scope.ServiceProvider.GetService<ILogger<Program>>();

                var settings = scope.ServiceProvider.GetService<ConfigServiceConfiguration>()
                                    ?.Authentication?.Kestrel;

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

                if (string.IsNullOrWhiteSpace(settings.Certificate))
                {
                    logger.LogError("no certificate given, provide path to a valid certificate with '" + $"{nameof(AuthenticationConfiguration)}:" +
                                    $"{nameof(AuthenticationConfiguration.Kestrel)}:" + $"{nameof(AuthenticationConfiguration.Kestrel.Certificate)}" + "'");
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

                if (string.IsNullOrWhiteSpace(settings.IpAddress))
                {
                    logger.LogError("no ip-address given, provide a valid ipv4 or ipv6 binding-address or 'localhost' for '" +
                                    $"{nameof(AuthenticationConfiguration)}" + $"{nameof(AuthenticationConfiguration.Kestrel)}" +
                                    $"{nameof(AuthenticationConfiguration.Kestrel.IpAddress)}" + "'");
                    return;
                }

                if (settings.IpAddress == "localhost")
                {
                    logger.LogInformation($"binding to: https://localhost:{settings.Port}");
                    options.ListenLocalhost(settings.Port, listenOptions => { listenOptions.UseHttps(connectionOptions); });
                }
                else
                {
                    var ip = IPAddress.Parse(settings.IpAddress);
                    logger.LogInformation($"binding to: https://{ip}:{settings.Port}");
                    options.Listen(ip, settings.Port, listenOptions => { listenOptions.UseHttps(connectionOptions); });
                }
            }
        }

        /// <summary>
        ///     Create and Configure a WebHostBuilder
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static IWebHostBuilder CreateWebHostBuilder(string[] args)
            => WebHost.CreateDefaultBuilder(args)
                      .UseMetrics()
                      .UseStartup<Startup>()
                      .ConfigureAppConfiguration(
                          (context, builder) =>
                          {
                              builder.Sources.Clear();
                              builder.AddJsonFile("appsettings.json", true, true)
                                     .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", true, true)
                                     .AddEnvironmentVariables()
                                     .AddEnvironmentVariables("MAV_CONFIG_")
                                     .AddCommandLine(args);
                          })
                      .ConfigureLogging((context, builder) =>
                      {
                          context.Configuration.ConfigureNLog();

                          builder.ClearProviders()
                                 .SetMinimumLevel(LogLevel.Trace)
                                 .AddNLog(context.Configuration.GetSection("LoggingConfiguration"));
                      })
                      .UseKestrel(ConfigureKestrelCertAuth);
    }
}