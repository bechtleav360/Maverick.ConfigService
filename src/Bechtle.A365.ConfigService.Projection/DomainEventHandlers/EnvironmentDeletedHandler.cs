using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class EnvironmentDeletedHandler : IDomainEventHandler<EnvironmentDeleted>
    {
        private readonly IConfigurationDatabase _database;

        /// <inheritdoc />
        public EnvironmentDeletedHandler(IConfigurationDatabase database)
        {
            _database = database;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(EnvironmentDeleted domainEvent) => await _database.DeleteEnvironment(domainEvent.Identifier);
    }
}