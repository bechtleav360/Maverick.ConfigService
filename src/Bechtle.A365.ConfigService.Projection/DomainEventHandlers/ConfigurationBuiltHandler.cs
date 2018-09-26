using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class ConfigurationBuiltHandler : IDomainEventHandler<ConfigurationBuilt>
    {
        private readonly IConfigurationDatabase _database;
        private readonly IConfigurationCompiler _compiler;

        /// <inheritdoc />
        public ConfigurationBuiltHandler(IConfigurationDatabase database, IConfigurationCompiler compiler)
        {
            _database = database;
            _compiler = compiler;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(ConfigurationBuilt domainEvent)
        {
        }
    }
}