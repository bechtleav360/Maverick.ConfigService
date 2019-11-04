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
        protected override bool ApplyEventInternal(StreamedEvent streamedEvent)
        {
            switch (streamedEvent.DomainEvent)
            {
                case StructureCreated created:
                    Identifiers.Add(created.Identifier);
                    return true;

                case StructureDeleted deleted:
                    if (Identifiers.Contains(deleted.Identifier))
                        Identifiers.Remove(deleted.Identifier);
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        protected override void ApplySnapshotInternal(StreamedObject streamedObject)
        {
            if (!(streamedObject is StreamedStructureList other))
                return;

            Identifiers = other.Identifiers;
        }
    }
}