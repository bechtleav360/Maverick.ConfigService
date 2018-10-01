using System.IO;
using System.Xml;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.Logging.NLog.Adapter;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService
{
    public class Program
    {
        public static void Main(string[] args) => CreateWebHostBuilder(args).Build().Run();

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
                              NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(xmlReader, null);

                          builder.ClearProviders()
                                 .AddProvider(new A365NLogProvider());
                      });
    }
}