using System;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     built from <see cref="ConfigStructure" /> with data from <see cref="ConfigEnvironment" />.
    /// </summary>
    public class ConfigSnapshot : DomainObject
    {
        private EnvironmentIdentifier _environment;
        private StructureIdentifier _structure;
        private DateTime? _validFrom;
        private DateTime? _validTo;

        /// <summary>
        ///     create events that Create this DomainObject when saved
        /// </summary>
        /// <returns></returns>
        public ConfigSnapshot Create()
        {
            RecordedEvents.Add(new ConfigurationBuilt(_environment, _structure, _validFrom, _validTo));

            return this;
        }

        /// <summary>
        ///     set the identifier for this <see cref="ConfigSnapshot" /> to the given Identifiers
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
        ///     set this Configuration to be valid from the given point in time, or always if null
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        public ConfigSnapshot ValidFrom(DateTime? from)
        {
            _validFrom = from;
            return this;
        }

        /// <summary>
        ///     set this Configuration to be valid up to the given point in time, or indefinitely if null
        /// </summary>
        /// <param name="to"></param>
        /// <returns></returns>
        public ConfigSnapshot ValidTo(DateTime? to)
        {
            _validTo = to;
            return this;
        }
    }
}