using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class EnvironmentCreatedHandler : IDomainEventHandler<EnvironmentCreated>
    {
        private readonly IConfigurationDatabase _database;
        private readonly ILogger<EnvironmentCreatedHandler> _logger;

        /// <inheritdoc />
        public EnvironmentCreatedHandler(IConfigurationDatabase database,
                                         ILogger<EnvironmentCreatedHandler> logger)
        {
            _database = database;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(EnvironmentCreated domainEvent)
        {
            if (domainEvent is null)
                throw new ArgumentNullException(nameof(domainEvent), $"{nameof(domainEvent)} must not be empty");

            _logger.LogInformation($"creating environment {domainEvent.Identifier}");

            await _database.CreateEnvironment(domainEvent.Identifier, false);
        }
    }
}