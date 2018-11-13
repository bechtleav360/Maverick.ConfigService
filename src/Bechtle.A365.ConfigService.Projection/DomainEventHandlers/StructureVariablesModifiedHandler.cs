using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class StructureVariablesModifiedHandler : IDomainEventHandler<StructureVariablesModified>
    {
        private readonly IConfigurationDatabase _database;

        /// <inheritdoc />
        public StructureVariablesModifiedHandler(IConfigurationDatabase database)
        {
            _database = database;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(StructureVariablesModified domainEvent) => await _database.ApplyChanges(domainEvent.Identifier,
                                                                                                                    domainEvent.ModifiedKeys);
    }
}