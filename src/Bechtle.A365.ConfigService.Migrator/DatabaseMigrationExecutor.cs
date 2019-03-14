using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Migrator
{
    /// <summary>
    ///     Execute Migrations on the given DatabaseContext <typeparamref name="T"/>
    /// </summary>
    public class DatabaseMigrationExecutor<T> : HostedService where T : DbContext
    {
        private readonly ILogger _logger;

        /// <inheritdoc />
        public DatabaseMigrationExecutor(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = serviceProvider.GetService<ILogger>() ??
                      serviceProvider.GetService<ILogger<DatabaseMigrationExecutor<T>>>();
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (var scope = GetNewScope())
                {
                    var configuration = scope.ServiceProvider.GetService<IConfiguration>();

                    _logger.LogInformation("effective configuration for migration:\r\n" +
                                           string.Join("\r\n", configuration.AsEnumerable().OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase)));

                    var context = scope.ServiceProvider.GetService<T>() ?? throw new ArgumentNullException(nameof(T));

                    await context.Database.MigrateAsync(cancellationToken);
                }

                _logger.LogInformation("Migration completed Successfully");
            }
            catch (Exception e)
            {
                _logger.LogError($"Error while running Migration: {e}");
            }
            finally
            {
                _logger.LogInformation("you can now close this program with ^C");
            }
        }
    }
}