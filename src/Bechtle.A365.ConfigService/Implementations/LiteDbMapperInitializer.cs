using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations.Stores;
using LiteDB;
using Microsoft.Extensions.Hosting;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     Setup the global instance of <see cref="BsonMapper" /> for <see cref="DomainObjectStore" />
    /// </summary>
    public class LiteDbMapperInitializer : BackgroundService
    {
        /// <inheritdoc />
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            BsonMapper.Global
                      .Entity<ConfigEnvironment>()
                      .Ignore(o => o.Json)
                      .Ignore(o => o.Keys)
                      .Ignore(o => o.KeyPaths);

            BsonMapper.Global
                      .Entity<ConfigStructure>()
                      .Ignore(o => o.Keys)
                      .Ignore(o => o.Variables);

            BsonMapper.Global
                      .Entity<EnvironmentLayer>()
                      .Ignore(o => o.Json)
                      .Ignore(o => o.Keys)
                      .Ignore(o => o.KeyPaths);

            BsonMapper.Global
                      .Entity<PreparedConfiguration>()
                      .Ignore(o => o.Json)
                      .Ignore(o => o.Keys)
                      .Ignore(o => o.UsedKeys)
                      .Ignore(o => o.Errors)
                      .Ignore(o => o.Warnings);

            return Task.CompletedTask;
        }
    }
}
