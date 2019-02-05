using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Bechtle.A365.ConfigService.Authentication.Certificates;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.Logging.NLog.Adapter;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
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
                          options.ListenLocalhost(5001, listenOptions =>
                          {
                              listenOptions.UseHttps(new HttpsConnectionAdapterOptions
                              {
                                  ServerCertificate = new X509Certificate2("localhost.pfx", "1"),
                                  ClientCertificateMode = ClientCertificateMode.RequireCertificate,
                                  ClientCertificateValidation = CertificateValidator.DisableChannelValidation
                              });
                          });
                      });
    }
}