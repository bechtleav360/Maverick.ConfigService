using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class DefaultEnvironmentCreatedHandler : IDomainEventHandler<DefaultEnvironmentCreated>
    {
        private readonly IConfigurationDatabase _database;

        /// <inheritdoc />
        public DefaultEnvironmentCreatedHandler(IConfigurationDatabase database)
        {
            _database = database;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(DefaultEnvironmentCreated domainEvent) => await _database.CreateEnvironment(domainEvent.Identifier, true);
    }
}