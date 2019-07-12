using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bechtle.A365.ConfigService.Projection
{
    public static class ServiceBaseLifetimeHostExtensions
    {
        public static Task RunAsServiceAsync(this IHostBuilder hostBuilder, CancellationToken cancellationToken = default)
            => hostBuilder.UseServiceBaseLifetime().Build().RunAsync(cancellationToken);

        public static IHostBuilder UseServiceBaseLifetime(this IHostBuilder hostBuilder) 
            => hostBuilder.ConfigureServices((hostContext, services) => services.AddSingleton<IHostLifetime, ServiceBaseLifetime>());
    }
}