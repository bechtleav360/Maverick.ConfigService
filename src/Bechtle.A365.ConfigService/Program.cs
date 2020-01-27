using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Formatters;
using Bechtle.A365.ConfigService.Authentication.Certificates;
using Bechtle.A365.ConfigService.Common.Utilities;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Utilities;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NLog.Extensions.Logging;

namespace Bechtle.A365.ConfigService
{
    /// <summary>
    ///     Main Entry-Point for the Application
    /// </summary>
    public static class Program
    {
        /// <summary>
        ///     Build the WebHost that runs this application
        /// </summary>
        /// <param name="args"></param>
        public static Task Main(string[] args) => CreateWebHostBuilder(args).Build().RunAsync();

        /// <summary>
        ///     Configure Kestrel to use Certificate-based Authentication
        /// </summary>
        /// <param name="options"></param>
        private static void ConfigureKestrelCertAuth(KestrelServerOptions options)
        {
            using var scope = options.ApplicationServices.CreateScope();

            var logger = scope.ServiceProvider
                              .GetService<ILoggerFactory>()
                              ?.CreateLogger(nameof(Program));

            var settings = scope.ServiceProvider
                                .GetService<IOptionsMonitor<KestrelAuthenticationConfiguration>>()
                                ?.CurrentValue;

            if (settings is null)
            {
                logger.LogWarning($"{nameof(KestrelAuthenticationConfiguration)} is null, can't configure kestrel");
                return;
            }

            if (!settings.Enabled)
            {
                logger.LogWarning("skipping configuration of kestrel");
                return;
            }

            if (string.IsNullOrWhiteSpace(settings.Certificate))
            {
                logger.LogError("no certificate given, provide path to a valid certificate with '" +
                                $"{nameof(KestrelAuthenticationConfiguration)}:{nameof(KestrelAuthenticationConfiguration.Certificate)}'");
                return;
            }

            X509Certificate2 certificate = null;
            if (Path.GetExtension(settings.Certificate) == "pfx")
                certificate = settings.Password is null
                                  ? new X509Certificate2(settings.Certificate)
                                  : new X509Certificate2(settings.Certificate, settings.Password);

            var certpath = Environment.GetEnvironmentVariable("ASPNETCORE_SSLCERT_PATH");
            if (!string.IsNullOrEmpty(certpath) || Path.GetExtension(settings.Certificate) == "crt")
            {
                var port = Environment.GetEnvironmentVariable("ASPNETCORE_SSL_PORT");
                certificate = X509CertificateUtility.LoadFromCrt(certpath) ?? X509CertificateUtility.LoadFromCrt(settings.Certificate);
                settings.Port = int.Parse(port ?? settings.Port.ToString());
            }

            var connectionOptions = new HttpsConnectionAdapterOptions
            {
                ServerCertificate = certificate
            };

            var inDocker = bool.Parse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") ?? "false");
            if (!inDocker)
            {
                logger.LogInformation("Not running in docker, adding client certificate validation");
                connectionOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                connectionOptions.ClientCertificateValidation = CertificateValidator.DisableChannelValidation;
            }

            logger.LogInformation($"loaded certificate: {connectionOptions.ServerCertificate}");

            if (string.IsNullOrWhiteSpace(settings.IpAddress))
            {
                logger.LogError("no ip-address given, provide a valid ipv4 or ipv6 binding-address or 'localhost' for '" +
                                $"{nameof(KestrelAuthenticationConfiguration)}:{nameof(KestrelAuthenticationConfiguration.IpAddress)}'");
                return;
            }

            if (settings.IpAddress == "localhost")
            {
                logger.LogInformation($"binding to: https://localhost:{settings.Port}");
                options.ListenLocalhost(settings.Port, listenOptions => { listenOptions.UseHttps(connectionOptions); });
            }
            else if (settings.IpAddress == "*")
            {
                logger.LogInformation($"binding to: https://*:{settings.Port}");
                options.ListenAnyIP(settings.Port, listenOptions => { listenOptions.UseHttps(connectionOptions); });
            }
            else
            {
                var ip = IPAddress.Parse(settings.IpAddress);
                logger.LogInformation($"binding to: https://{ip}:{settings.Port}");
                options.Listen(ip, settings.Port, listenOptions => { listenOptions.UseHttps(connectionOptions); });
            }
        }

        private static void ConfigureMetrics(WebHostBuilderContext context, IMetricsBuilder builder)
        {
            builder.OutputMetrics.AsPlainText(options => { options.Encoding = Encoding.UTF8; })
                   .OutputMetrics.AsJson(options =>
                   {
                       options.SerializerSettings.FloatFormatHandling = FloatFormatHandling.DefaultValue;
                       options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                   })
                   .OutputMetrics.Using<CustomMetricsFormatter>();
        }

        /// <summary>
        ///     Create and Configure a WebHostBuilder
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static IWebHostBuilder CreateWebHostBuilder(string[] args)
            => WebHost.CreateDefaultBuilder(args)
                      // configure custom formatter
                      .ConfigureMetrics(ConfigureMetrics)
                      // following three calls replace UseMetrics()
                      .ConfigureServices((context, services) =>
                      {
                          services.AddMetricsReportingHostedService();
                          services.AddMetricsEndpoints(context.Configuration);
                          services.AddMetricsTrackingMiddleware(context.Configuration);
                      })
                      .UseMetricsEndpoints(options =>
                      {
                          var customFormatter = options.MetricsOutputFormatters.GetType<CustomMetricsFormatter>();
                          if (!(customFormatter is null))
                              options.MetricsEndpointOutputFormatter = customFormatter;
                      })
                      .UseMetricsWebTracking()
                      .UseStartup<Startup>()
                      .ConfigureAppConfiguration(
                          (context, builder) =>
                          {
                              builder.Sources.Clear();
                              builder.AddJsonFile("appsettings.json", true, true)
                                     .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", true, true)
                                     .AddEnvironmentVariables()
                                     .AddEnvironmentVariables("MAV_CONFIG_")
                                     .AddCommandLine(args)
                                     .AddJsonFile("data/appsettings.json", true, true);
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