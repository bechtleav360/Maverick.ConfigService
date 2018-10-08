using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <inheritdoc />
    /// <summary>
    ///     a Configuration has been built with information from <see cref="EnvironmentIdentifier" /> and <see cref="StructureIdentifier" />
    /// </summary>
    public class ConfigurationBuilt : DomainEvent
    {
        /// <inheritdoc />
        public ConfigurationBuilt(EnvironmentIdentifier environment,
                                  StructureIdentifier structure,
                                  DateTime? validFrom,
                                  DateTime? validTo)
        {
            Identifier = new ConfigurationIdentifier(environment, structure);
            ValidFrom = validFrom;
            ValidTo = validTo;
        }

        /// <inheritdoc />
        public ConfigurationBuilt()
        {
        }

        /// <inheritdoc cref="ConfigurationIdentifier" />
        public ConfigurationIdentifier Identifier { get; set; }

        /// <summary>
        ///     This Configuration is to be Valid from the given point in time, or always if null
        /// </summary>
        public DateTime? ValidFrom { get; set; }

        /// <summary>
        ///     This Configuration is to be Valid up to the given point in time, or indefinitely if null
        /// </summary>
        public DateTime? ValidTo { get; set; }
    }
}