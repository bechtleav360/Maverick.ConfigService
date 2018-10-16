using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class StructureCreatedHandler : IDomainEventHandler<StructureCreated>
    {
        private readonly IConfigurationDatabase _database;

        /// <inheritdoc />
        public StructureCreatedHandler(IConfigurationDatabase database)
        {
            _database = database;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(StructureCreated domainEvent) => await _database.CreateStructure(domainEvent.Identifier,
                                                                                                             domainEvent.Keys,
                                                                                                             domainEvent.Variables);
    }
}