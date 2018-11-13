using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Configuration-Structure containing the overall structure and default values of a configurations.
    ///     Must be combined with a <see cref="ConfigEnvironment" /> to produce a <see cref="ConfigSnapshot" />
    /// </summary>
    public class ConfigStructure : DomainObject
    {
        private StructureIdentifier _identifier;

        /// <summary>
        ///     set the identifier for this <see cref="ConfigStructure"/> to the given identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public ConfigStructure IdentifiedBy(StructureIdentifier identifier)
        {
            _identifier = identifier;

            return this;
        }

        /// <summary>
        ///     create events that Create this DomainObject when saved
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        public ConfigStructure Create(IDictionary<string, string> keys, IDictionary<string, string> variables)
        {
            RecordedEvents.Add(new StructureCreated(_identifier, keys, variables));

            return this;
        }

        /// <summary>
        ///     create events that Delete this DomainObject when saved
        /// </summary>
        /// <returns></returns>
        public ConfigStructure Delete()
        {
            RecordedEvents.Add(new StructureDeleted(_identifier));

            return this;
        }

        /// <summary>
        ///     create events that modify the variables associated to this ConfigStructure
        /// </summary>
        /// <param name="actions"></param>
        /// <returns></returns>
        public ConfigStructure ModifyVariables(IList<ConfigKeyAction> actions)
        {
            RecordedEvents.Add(new StructureVariablesModified(_identifier, actions.ToArray()));
            return this;
        }
    }
}