using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class StructureDeletedHandler : IDomainEventHandler<StructureDeleted>
    {
        private readonly IConfigurationDatabase _database;

        /// <inheritdoc />
        public StructureDeletedHandler(IConfigurationDatabase database)
        {
            _database = database;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(StructureDeleted domainEvent)
        {
        }
    }
}