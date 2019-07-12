using Microsoft.Extensions.Configuration;

namespace Bechtle.A365.ConfigService.Cli
{
    public class ApplicationSettings
    {
        public string ConnectionString { get; set; }

        public DbBackend DbType { get; set; }

        public IConfiguration EffectiveConfiguration { get; set; }
    }
}