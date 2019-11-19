using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Helper-Domain-Object to access all available Environments
    /// </summary>
    public class ConfigEnvironmentList : DomainObject
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
        protected override void ApplySnapshotInternal(DomainObject domainObject)
        {
            if (!(domainObject is ConfigEnvironmentList other))
                return;

            Identifiers = other.Identifiers;
        }

        /// <inheritdoc />
        protected override IDictionary<Type, Func<ReplayedEvent, bool>> GetEventApplicationMapping()
            => new Dictionary<Type, Func<ReplayedEvent, bool>>
            {
                {typeof(DefaultEnvironmentCreated), HandleDefaultEnvironmentCreatedEvent},
                {typeof(EnvironmentCreated), HandleEnvironmentCreatedEvent},
                {typeof(EnvironmentDeleted), HandleEnvironmentDeletedEvent}
            };

        private bool HandleDefaultEnvironmentCreatedEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is DefaultEnvironmentCreated created))
                return false;

            Identifiers.Add(created.Identifier);
            return true;
        }

        private bool HandleEnvironmentCreatedEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is EnvironmentCreated created))
                return false;

            Identifiers.Add(created.Identifier);
            return true;
        }

        private bool HandleEnvironmentDeletedEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is EnvironmentDeleted deleted))
                return false;

            if (Identifiers.Contains(deleted.Identifier))
                Identifiers.Remove(deleted.Identifier);
            return true;
        }
    }
}