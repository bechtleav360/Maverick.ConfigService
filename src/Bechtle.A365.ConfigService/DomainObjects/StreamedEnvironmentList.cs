﻿using System.Collections.Generic;
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
        protected override void ApplySnapshotInternal(StreamedObject streamedObject)
        {
            if (!(streamedObject is StreamedEnvironmentList other))
                return;

            Identifiers = other.Identifiers;
        }
    }
}