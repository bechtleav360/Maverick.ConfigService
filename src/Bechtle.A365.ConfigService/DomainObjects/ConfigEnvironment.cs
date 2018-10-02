using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Bechtle.A365.ConfigService.Services;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    public abstract class DomainObject
    {
        /// <summary>
        /// </summary>
        /// <param name="store"></param>
        public DomainObject()
        {
            RecordedEvents = new List<DomainEvent>();
        }

        /// <summary>
        ///     Events that lead to the DomainObject having the current state (Created, Modified, Deleted)
        /// </summary>
        protected IList<DomainEvent> RecordedEvents { get; }

        public virtual void Save(IEventStore store)
        {
            foreach (var @event in RecordedEvents) store.WriteEvent(@event);
        }
    }

    /// <summary>
    ///     Configuration-Environment containing sections of configuration that are shared among many <see cref="ConfigStructure" />
    /// </summary>
    public class ConfigEnvironment : DomainObject
    {
        private EnvironmentIdentifier _identifier;
        private bool _isDefault;

        public ConfigEnvironment IdentifiedBy(EnvironmentIdentifier identifier, bool isDefault = false)
        {
            _identifier = identifier;
            if (isDefault)
            {
                _isDefault = true;
                _identifier.Name = "Default";
            }

            return this;
        }

        public ConfigEnvironment DefaultIdentifiedBy(string category) => IdentifiedBy(new EnvironmentIdentifier(category, "Default"), true);

        public ConfigEnvironment Create()
        {
            if (_isDefault)
                RecordedEvents.Add(new DefaultEnvironmentCreated(_identifier));
            else
                RecordedEvents.Add(new EnvironmentCreated(_identifier));

            return this;
        }

        public ConfigEnvironment Delete()
        {
            if (!_isDefault)
                RecordedEvents.Add(new EnvironmentDeleted(_identifier));

            return this;
        }

        public ConfigEnvironment ModifyKeys(IEnumerable<ConfigKeyAction> actions)
        {
            RecordedEvents.Add(new EnvironmentKeysModified(_identifier, actions.ToArray()));

            return this;
        }
    }

    /// <summary>
    ///     Configuration-Structure containing the overall structure and default values of a configurations.
    ///     Must be combined with a <see cref="ConfigEnvironment" /> to produce a <see cref="ConfigSnapshot" />
    /// </summary>
    public class ConfigStructure : DomainObject
    {
        private StructureIdentifier _identifier;

        public ConfigStructure IdentifiedBy(StructureIdentifier identifier)
        {
            _identifier = identifier;

            return this;
        }

        public ConfigStructure Create(IEnumerable<ConfigKeyAction> actions)
        {
            RecordedEvents.Add(new StructureCreated(_identifier, actions.ToArray()));

            return this;
        }

        public ConfigStructure Delete()
        {
            RecordedEvents.Add(new StructureDeleted(_identifier));
            
            return this;
        }
    }

    /// <summary>
    ///     built from <see cref="ConfigStructure" /> with data from <see cref="ConfigEnvironment" />.
    /// </summary>
    public class ConfigSnapshot : DomainObject
    {
        private StructureIdentifier _structure;
        private EnvironmentIdentifier _environment;

        public ConfigSnapshot IdentifiedBy(StructureIdentifier structure, EnvironmentIdentifier environment)
        {
            _structure = structure;
            _environment = environment;

            return this;
        }

        public ConfigSnapshot Create()
        {
            RecordedEvents.Add(new ConfigurationBuilt(_environment, _structure));

            return this;
        }
    }
}