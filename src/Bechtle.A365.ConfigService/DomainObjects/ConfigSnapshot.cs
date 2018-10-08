using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     built from <see cref="ConfigStructure" /> with data from <see cref="ConfigEnvironment" />.
    /// </summary>
    public class ConfigSnapshot : DomainObject
    {
        private StructureIdentifier _structure;
        private EnvironmentIdentifier _environment;

        /// <summary>
        ///     set the identifier for this <see cref="ConfigSnapshot"/> to the given Identifiers
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="environment"></param>
        /// <returns></returns>
        public ConfigSnapshot IdentifiedBy(StructureIdentifier structure, EnvironmentIdentifier environment)
        {
            _structure = structure;
            _environment = environment;

            return this;
        }

        /// <summary>
        ///     create events that Create this DomainObject when saved
        /// </summary>
        /// <returns></returns>
        public ConfigSnapshot Create()
        {
            RecordedEvents.Add(new ConfigurationBuilt(_environment, _structure));

            return this;
        }
    }
}