using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Helper-Domain-Object to access all available Environments
    /// </summary>
    public class EnvironmentLayerList : DomainObject
    {
        /// <summary>
        ///     internal Lookup to keep track of Environments
        /// </summary>
        public HashSet<LayerIdentifier> Identifiers { get; set; } = new HashSet<LayerIdentifier>();

        /// <inheritdoc />
        public override long CalculateCacheSize()
            => Identifiers?.Count * 10 ?? 0;

        /// <summary>
        ///     get a list of all active Environment-Identifiers
        /// </summary>
        /// <returns></returns>
        public ICollection<LayerIdentifier> GetIdentifiers() => Identifiers.ToList();

        /// <inheritdoc />
        protected override void ApplySnapshotInternal(DomainObject domainObject)
        {
            if (!(domainObject is EnvironmentLayerList other))
                return;

            Identifiers = other.Identifiers;
        }

        /// <inheritdoc />
        protected override IDictionary<Type, Func<ReplayedEvent, bool>> GetEventApplicationMapping()
            => new Dictionary<Type, Func<ReplayedEvent, bool>>
            {
                {typeof(EnvironmentLayerCreated), HandleEnvironmentLayerCreatedEvent},
                {typeof(EnvironmentLayerDeleted), HandleEnvironmentLayerDeletedEvent},
            };

        private bool HandleEnvironmentLayerCreatedEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is EnvironmentLayerCreated created))
                return false;

            Identifiers.Add(created.Identifier);
            return true;
        }

        private bool HandleEnvironmentLayerDeletedEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is EnvironmentLayerDeleted deleted))
                return false;

            if (Identifiers.Contains(deleted.Identifier))
                Identifiers.Remove(deleted.Identifier);
            return true;
        }
    }
}