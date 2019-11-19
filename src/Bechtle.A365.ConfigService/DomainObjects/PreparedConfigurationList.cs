using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Helper-Domain-Object to access all built Configurations
    /// </summary>
    public class PreparedConfigurationList : DomainObject
    {
        /// <summary>
        ///     internal Lookup, to keep data for Configurations in
        /// </summary>
        protected Dictionary<ConfigurationIdentifier, ConfigInformation> Lookup { get; set; }
            = new Dictionary<ConfigurationIdentifier, ConfigInformation>();

        // 10 for each Identifier, plus 5 for rest
        /// <inheritdoc />
        public override long CalculateCacheSize()
            => Lookup?.Count * 25 ?? 0;

        /// <summary>
        ///     get a list of all active Environment-Identifiers
        /// </summary>
        /// <returns></returns>
        public IDictionary<ConfigurationIdentifier, (DateTime? ValidFrom, DateTime? ValidTo)> GetIdentifiers()
            => Lookup.ToDictionary(_ => _.Key, _ => (_.Value.ValidFrom, _.Value.ValidTo));

        /// <summary>
        ///     get a list of <see cref="ConfigurationIdentifier" /> for all stale Configurations
        /// </summary>
        /// <returns></returns>
        public IList<ConfigurationIdentifier> GetStale() => Lookup.Values
                                                                  .Where(e => e.Stale)
                                                                  .Select(e => e.Identifier)
                                                                  .ToList();

        /// <inheritdoc />
        protected override void ApplySnapshotInternal(DomainObject domainObject)
        {
            if (!(domainObject is PreparedConfigurationList other))
                return;

            Lookup = other.Lookup;
        }

        /// <inheritdoc />
        protected override IDictionary<Type, Func<StreamedEvent, bool>> GetEventApplicationMapping()
            => new Dictionary<Type, Func<StreamedEvent, bool>>
            {
                {typeof(ConfigurationBuilt), HandleConfigurationBuiltEvent},
                {typeof(EnvironmentKeysImported), HandleEnvironmentKeysImportedEvent},
                {typeof(EnvironmentKeysModified), HandleEnvironmentKeysModifiedEvent},
                {typeof(StructureVariablesModified), HandleStructureVariablesModifiedEvent}
            };

        private bool HandleConfigurationBuiltEvent(StreamedEvent streamedEvent)
        {
            if (!(streamedEvent.DomainEvent is ConfigurationBuilt built))
                return false;

            Lookup[built.Identifier] = new ConfigInformation
            {
                Identifier = built.Identifier,
                Stale = false,
                ValidFrom = built.ValidFrom,
                ValidTo = built.ValidTo
            };
            return true;
        }

        private bool HandleEnvironmentKeysImportedEvent(StreamedEvent streamedEvent)
        {
            if (!(streamedEvent.DomainEvent is EnvironmentKeysImported imported))
                return false;

            foreach (var (_, info) in Lookup.Where(l => l.Value.UsedEnvironment == imported.Identifier))
                info.Stale = true;

            return true;
        }

        private bool HandleEnvironmentKeysModifiedEvent(StreamedEvent streamedEvent)
        {
            if (!(streamedEvent.DomainEvent is EnvironmentKeysModified modified1))
                return false;

            foreach (var (_, info) in Lookup.Where(l => l.Value.UsedEnvironment == modified1.Identifier))
                info.Stale = true;

            return true;
        }

        private bool HandleStructureVariablesModifiedEvent(StreamedEvent streamedEvent)
        {
            if (!(streamedEvent.DomainEvent is StructureVariablesModified modified2))
                return false;

            foreach (var (_, info) in Lookup.Where(l => l.Value.UsedStructure == modified2.Identifier))
                info.Stale = true;

            return true;
        }

        /// <summary>
        ///     internal information for any given Configuration
        /// </summary>
        protected class ConfigInformation
        {
            /// <Inheritdoc cref="ConfigurationIdentifier" />
            public ConfigurationIdentifier Identifier { get; set; }

            /// <summary>
            ///     Indicates that the Environment or Structure used to build this Configuration has been modified after it was built.
            ///     This is currently only an indicator, because we can't identify the exact keys used to build the Config.
            /// </summary>
            public bool Stale { get; set; }

            /// <Inheritdoc cref="EnvironmentIdentifier" />
            public EnvironmentIdentifier UsedEnvironment => Identifier.Environment;

            /// <Inheritdoc cref="StructureIdentifier" />
            public StructureIdentifier UsedStructure => Identifier.Structure;

            /// <Inheritdoc cref="PreparedConfiguration.ValidFrom" />
            public DateTime? ValidFrom { get; set; }

            /// <Inheritdoc cref="PreparedConfiguration.ValidTo" />
            public DateTime? ValidTo { get; set; }
        }
    }
}