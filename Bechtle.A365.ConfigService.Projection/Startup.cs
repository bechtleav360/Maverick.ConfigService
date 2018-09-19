using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ILoggerFactory>(provider => new LoggerFactory().AddConsole(LogLevel.Trace))
                    .AddSingleton<IConfiguration>(Configuration)
                    .AddSingleton<ProjectionConfiguration>(provider => provider.GetService<IConfiguration>()
                                                                               .Get<ProjectionConfiguration>())
                    .AddSingleton<IProjection, Projection>();
        }
    }
}