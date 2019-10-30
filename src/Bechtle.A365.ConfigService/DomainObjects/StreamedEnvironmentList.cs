using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    public class StreamedEnvironmentList : StreamedObject
    {
        protected HashSet<EnvironmentIdentifier> Identifiers { get; set; } = new HashSet<EnvironmentIdentifier>();

        /// <inheritdoc />
        protected override bool ApplyEventInternal(StreamedEvent streamedEvent)
        {
            switch (streamedEvent.DomainEvent)
            {
                case DefaultEnvironmentCreated created:
                    Identifiers.Add(created.Identifier);
                    return true;

                case EnvironmentCreated created:
                    Identifiers.Add(created.Identifier);
                    return true;

                case EnvironmentDeleted deleted:
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

            var other = JsonSerializer.Deserialize<StreamedEnvironmentList>(snapshot.Data);

            Identifiers = other.Identifiers;
        }

        /// <inheritdoc />
        public override long CalculateCacheSize()
            => Identifiers?.Count * 10 ?? 0;

        /// <summary>
        ///     get a list of all active Environment-Identifiers
        /// </summary>
        /// <returns></returns>
        public ICollection<EnvironmentIdentifier> GetIdentifiers() => Identifiers.ToList();
    }
}