using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class EnvironmentKeyModifiedHandler : IDomainEventHandler<EnvironmentKeysModified>
    {
        private readonly IConfigurationDatabase _database;

        /// <inheritdoc />
        public EnvironmentKeyModifiedHandler(IConfigurationDatabase database)
        {
            _database = database;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(EnvironmentKeysModified domainEvent)
        {
        }
    }
}