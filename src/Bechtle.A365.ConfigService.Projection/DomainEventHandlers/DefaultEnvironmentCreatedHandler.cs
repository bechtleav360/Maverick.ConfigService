using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class DefaultEnvironmentCreatedHandler : IDomainEventHandler<DefaultEnvironmentCreated>
    {
        private readonly IConfigurationDatabase _database;
        private readonly ILogger<DefaultEnvironmentCreatedHandler> _logger;

        /// <inheritdoc />
        public DefaultEnvironmentCreatedHandler(IConfigurationDatabase database,
                                                ILogger<DefaultEnvironmentCreatedHandler> logger)
        {
            _database = database;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(DefaultEnvironmentCreated domainEvent)
        {
            _logger.LogInformation($"creating default environment {domainEvent.Identifier}");

            await _database.CreateEnvironment(domainEvent.Identifier, true);
        }
    }
}