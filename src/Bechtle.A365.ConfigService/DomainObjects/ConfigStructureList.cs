using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Helper-Domain-Object to access all available Structures
    /// </summary>
    public class ConfigStructureList : DomainObject
    {
        /// <summary>
        ///     internal Lookup to keep track of Structures
        /// </summary>
        public HashSet<StructureIdentifier> Identifiers { get; set; } = new HashSet<StructureIdentifier>();

        /// <inheritdoc />
        public override long CalculateCacheSize()
            => Identifiers?.Count * 10 ?? 0;

        /// <summary>
        ///     get a list of all active Environment-Identifiers
        /// </summary>
        /// <returns></returns>
        public ICollection<StructureIdentifier> GetIdentifiers() => Identifiers.ToList();

        /// <inheritdoc />
        protected override void ApplySnapshotInternal(DomainObject domainObject)
        {
            if (!(domainObject is ConfigStructureList other))
                return;

            Identifiers = other.Identifiers;
        }

        /// <inheritdoc />
        protected override IDictionary<Type, Func<ReplayedEvent, bool>> GetEventApplicationMapping()
            => new Dictionary<Type, Func<ReplayedEvent, bool>>
            {
                {typeof(StructureCreated), HandleStructureCreatedEvent},
                {typeof(StructureDeleted), HandleStructureDeletedEvent}
            };

        private bool HandleStructureCreatedEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is StructureCreated created))
                return false;

            Identifiers.Add(created.Identifier);
            return true;
        }

        private bool HandleStructureDeletedEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is StructureDeleted deleted))
                return false;

            if (Identifiers.Contains(deleted.Identifier))
                Identifiers.Remove(deleted.Identifier);
            return true;
        }
    }
}