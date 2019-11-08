using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Helper-Domain-Object to access all available Environments
    /// </summary>
    public class StreamedEnvironmentList : StreamedObject
    {
        /// <summary>
        ///     internal Lookup to keep track of Environments
        /// </summary>
        protected HashSet<EnvironmentIdentifier> Identifiers { get; set; } = new HashSet<EnvironmentIdentifier>();

        /// <inheritdoc />
        public override long CalculateCacheSize()
            => Identifiers?.Count * 10 ?? 0;

        /// <summary>
        ///     get a list of all active Environment-Identifiers
        /// </summary>
        /// <returns></returns>
        public ICollection<EnvironmentIdentifier> GetIdentifiers() => Identifiers.ToList();

        /// <inheritdoc />
        protected override void ApplySnapshotInternal(StreamedObject streamedObject)
        {
            if (!(streamedObject is StreamedEnvironmentList other))
                return;

            Identifiers = other.Identifiers;
        }

        /// <inheritdoc />
        protected override IDictionary<Type, Func<StreamedEvent, bool>> GetEventApplicationMapping()
            => new Dictionary<Type, Func<StreamedEvent, bool>>
            {
                {typeof(DefaultEnvironmentCreated), HandleDefaultEnvironmentCreatedEvent},
                {typeof(EnvironmentCreated), HandleEnvironmentCreatedEvent},
                {typeof(EnvironmentDeleted), HandleEnvironmentDeletedEvent}
            };

        private bool HandleDefaultEnvironmentCreatedEvent(StreamedEvent streamedEvent)
        {
            if (!(streamedEvent.DomainEvent is DefaultEnvironmentCreated created))
                return false;

            Identifiers.Add(created.Identifier);
            return true;
        }

        private bool HandleEnvironmentCreatedEvent(StreamedEvent streamedEvent)
        {
            if (!(streamedEvent.DomainEvent is EnvironmentCreated created))
                return false;

            Identifiers.Add(created.Identifier);
            return true;
        }

        private bool HandleEnvironmentDeletedEvent(StreamedEvent streamedEvent)
        {
            if (!(streamedEvent.DomainEvent is EnvironmentDeleted deleted))
                return false;

            if (Identifiers.Contains(deleted.Identifier))
                Identifiers.Remove(deleted.Identifier);
            return true;
        }
    }
}