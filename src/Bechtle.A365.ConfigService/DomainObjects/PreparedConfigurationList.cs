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
        public Dictionary<EnvironmentIdentifier, List<LayerIdentifier>> EnvironmentLayers { get; set; }
            = new Dictionary<EnvironmentIdentifier, List<LayerIdentifier>>();

        /// <summary>
        ///     internal InformationLookup for IdentifierLookup
        /// </summary>
        public Dictionary<string, ConfigurationIdentifier> IdentifierLookup { get; set; } = new Dictionary<string, ConfigurationIdentifier>();

        /// <summary>
        ///     internal InformationLookup for Config-Information
        /// </summary>
        public Dictionary<string, ConfigInformation> InfoLookup { get; set; } = new Dictionary<string, ConfigInformation>();

        // 10 for each Identifier, plus 5 for rest
        /// <inheritdoc />
        public override long CalculateCacheSize()
            => InfoLookup?.Count * 25 ?? 0;

        /// <summary>
        ///     get a list of all active Environment-Identifiers
        /// </summary>
        /// <returns></returns>
        public IDictionary<ConfigurationIdentifier, (DateTime? ValidFrom, DateTime? ValidTo)> GetIdentifiers()
            => InfoLookup.ToDictionary(_ => IdentifierLookup[_.Key], _ => (_.Value.ValidFrom, _.Value.ValidTo));

        /// <summary>
        ///     get a list of <see cref="ConfigurationIdentifier" /> for all stale Configurations
        /// </summary>
        /// <returns></returns>
        public IList<ConfigurationIdentifier> GetStale() => InfoLookup.Values
                                                                      .Where(e => e.Stale)
                                                                      .Select(e => e.Identifier)
                                                                      .ToList();

        /// <inheritdoc />
        protected override void ApplySnapshotInternal(DomainObject domainObject)
        {
            if (!(domainObject is PreparedConfigurationList other))
                return;

            IdentifierLookup = other.IdentifierLookup;
            InfoLookup = other.InfoLookup;
        }

        /// <inheritdoc />
        protected override IDictionary<Type, Func<ReplayedEvent, bool>> GetEventApplicationMapping()
            => new Dictionary<Type, Func<ReplayedEvent, bool>>
            {
                {typeof(ConfigurationBuilt), HandleConfigurationBuiltEvent},
                {typeof(EnvironmentLayersModified), HandleEnvironmentLayersModifiedEvent},
                {typeof(EnvironmentLayerKeysModified), HandleEnvironmentLayerKeysModifiedEvent},
                {typeof(EnvironmentLayerKeysImported), HandleEnvironmentLayerKeysImportedEvent},
                {typeof(StructureVariablesModified), HandleStructureVariablesModifiedEvent}
            };

        private bool HandleConfigurationBuiltEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is ConfigurationBuilt built))
                return false;

            var key = built.Identifier.ToString();

            IdentifierLookup[key] = built.Identifier;

            InfoLookup[key] = new ConfigInformation
            {
                Identifier = built.Identifier,
                Stale = false,
                ValidFrom = built.ValidFrom,
                ValidTo = built.ValidTo
            };

            return true;
        }

        private bool HandleEnvironmentLayerKeysImportedEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is EnvironmentLayerKeysImported imported))
                return false;

            var staleEnvs = EnvironmentLayers.Where(kvp => kvp.Value.Contains(imported.Identifier)).Select(kvp => kvp.Key)
                                             .Distinct()
                                             .ToList();

            foreach (var info in InfoLookup.Values)
                if (staleEnvs.Contains(info.UsedEnvironment))
                    info.Stale = true;

            return true;
        }

        private bool HandleEnvironmentLayerKeysModifiedEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is EnvironmentLayerKeysModified modified))
                return false;

            var staleEnvs = EnvironmentLayers.Where(kvp => kvp.Value.Contains(modified.Identifier)).Select(kvp => kvp.Key)
                                             .Distinct()
                                             .ToList();

            foreach (var info in InfoLookup.Values)
                if (staleEnvs.Contains(info.UsedEnvironment))
                    info.Stale = true;

            return true;
        }

        private bool HandleEnvironmentLayersModifiedEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is EnvironmentLayersModified modified))
                return false;

            EnvironmentLayers[modified.Identifier] = modified.Layers;

            return true;
        }

        private bool HandleStructureVariablesModifiedEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is StructureVariablesModified modified))
                return false;

            foreach (var (_, info) in InfoLookup.Where(l => l.Value.UsedStructure == modified.Identifier))
                info.Stale = true;

            return true;
        }

        /// <summary>
        ///     internal information for any given Configuration
        /// </summary>
        public class ConfigInformation
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