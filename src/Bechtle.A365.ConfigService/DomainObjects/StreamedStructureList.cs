using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    public class StreamedStructureList : StreamedObject
    {
        protected HashSet<StructureIdentifier> Identifiers { get; set; } = new HashSet<StructureIdentifier>();

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
        public override void ApplySnapshot(StreamedObjectSnapshot snapshot)
        {
            if (snapshot.DataType != GetType().Name)
                return;

            var other = JsonSerializer.Deserialize<StreamedStructureList>(snapshot.Data);

            Identifiers = other.Identifiers;
        }

        /// <inheritdoc />
        protected override long CalculateCacheSize()
            => Identifiers.Count * 10;

        /// <summary>
        ///     get a list of all active Environment-Identifiers
        /// </summary>
        /// <returns></returns>
        public ICollection<StructureIdentifier> GetIdentifiers() => Identifiers.ToList();
    }
}