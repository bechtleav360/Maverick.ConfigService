using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    public class StreamedConfigurationList : StreamedObject
    {
        protected Dictionary<ConfigurationIdentifier, ConfigInformation> Lookup { get; set; }
            = new Dictionary<ConfigurationIdentifier, ConfigInformation>();

        /// <inheritdoc />
        protected override bool ApplyEventInternal(StreamedEvent streamedEvent)
        {
            switch (streamedEvent.DomainEvent)
            {
                case ConfigurationBuilt built:
                    Lookup[built.Identifier] = new ConfigInformation
                    {
                        Identifier = built.Identifier,
                        Stale = false,
                        ValidFrom = built.ValidFrom,
                        ValidTo = built.ValidTo
                    };
                    return true;

                case EnvironmentKeysImported imported:
                    foreach (var (_, info) in Lookup.Where(l => l.Value.UsedEnvironment == imported.Identifier))
                        info.Stale = true;
                    return true;

                case EnvironmentKeysModified modified:
                    foreach (var (_, info) in Lookup.Where(l => l.Value.UsedEnvironment == modified.Identifier))
                        info.Stale = true;
                    return true;

                case StructureVariablesModified modified:
                    foreach (var (_, info) in Lookup.Where(l => l.Value.UsedStructure == modified.Identifier))
                        info.Stale = true;
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override void ApplySnapshot(StreamedObjectSnapshot snapshot)
        {
            if (snapshot.DataType != GetType().Name)
                return;

            var other = JsonSerializer.Deserialize<StreamedConfigurationList>(snapshot.Data);

            Lookup = other.Lookup;
        }

        /// <summary>
        ///     get a list of all active Environment-Identifiers
        /// </summary>
        /// <returns></returns>
        public IDictionary<ConfigurationIdentifier, (DateTime? ValidFrom, DateTime? ValidTo)> GetIdentifiers()
            => Lookup.ToDictionary(_ => _.Key, _ => (_.Value.ValidFrom, _.Value.ValidTo));

        /// <summary>
        ///     get the Staleness-Information for the given <see cref="ConfigurationIdentifier"/>
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public bool IsStale(ConfigurationIdentifier identifier)
            => Lookup.ContainsKey(identifier) && Lookup[identifier].Stale;

        /// <summary>
        ///     get a list of <see cref="ConfigurationIdentifier"/> for all stale Configurations
        /// </summary>
        /// <returns></returns>
        public IList<ConfigurationIdentifier> GetStale() => Lookup.Values
                                                                  .Where(e => e.Stale)
                                                                  .Select(e => e.Identifier)
                                                                  .ToList();

        /// <summary>
        ///     internal information for any given Configuration
        /// </summary>
        protected class ConfigInformation
        {
            /// <Inheritdoc cref="ConfigurationIdentifier"/>
            public ConfigurationIdentifier Identifier { get; set; }

            /// <Inheritdoc cref="StreamedConfiguration.ValidFrom"/>
            public DateTime? ValidFrom { get; set; }

            /// <Inheritdoc cref="StreamedConfiguration.ValidTo"/>
            public DateTime? ValidTo { get; set; }

            /// <Inheritdoc cref="EnvironmentIdentifier"/>
            public EnvironmentIdentifier UsedEnvironment => Identifier.Environment;

            /// <Inheritdoc cref="StructureIdentifier"/>
            public StructureIdentifier UsedStructure => Identifier.Structure;

            /// <summary>
            ///     Indicates that the Environment or Structure used to build this Configuration has been modified after it was built.
            ///     This is currently only an indicator, because we can't identify the exact keys used to build the Config.
            /// </summary>
            public bool Stale { get; set; }
        }
    }
}