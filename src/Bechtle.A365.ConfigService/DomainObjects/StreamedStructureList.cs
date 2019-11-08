using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Helper-Domain-Object to access all available Structures
    /// </summary>
    public class StreamedStructureList : StreamedObject
    {
        /// <summary>
        ///     internal Lookup to keep track of Structures
        /// </summary>
        protected HashSet<StructureIdentifier> Identifiers { get; set; } = new HashSet<StructureIdentifier>();

        /// <inheritdoc />
        public override long CalculateCacheSize()
            => Identifiers?.Count * 10 ?? 0;

        /// <summary>
        ///     get a list of all active Environment-Identifiers
        /// </summary>
        /// <returns></returns>
        public ICollection<StructureIdentifier> GetIdentifiers() => Identifiers.ToList();

        /// <inheritdoc />
        protected override void ApplySnapshotInternal(StreamedObject streamedObject)
        {
            if (!(streamedObject is StreamedStructureList other))
                return;

            Identifiers = other.Identifiers;
        }

        /// <inheritdoc />
        protected override IDictionary<Type, Func<StreamedEvent, bool>> GetEventApplicationMapping()
            => new Dictionary<Type, Func<StreamedEvent, bool>>
            {
                {typeof(StructureCreated), HandleStructureCreatedEvent},
                {typeof(StructureDeleted), HandleStructureDeletedEvent}
            };

        private bool HandleStructureCreatedEvent(StreamedEvent streamedEvent)
        {
            if (!(streamedEvent.DomainEvent is StructureCreated created))
                return false;

            Identifiers.Add(created.Identifier);
            return true;
        }

        private bool HandleStructureDeletedEvent(StreamedEvent streamedEvent)
        {
            if (!(streamedEvent.DomainEvent is StructureDeleted deleted))
                return false;

            if (Identifiers.Contains(deleted.Identifier))
                Identifiers.Remove(deleted.Identifier);
            return true;
        }
    }
}