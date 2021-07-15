using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Interfaces;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     base-class for all Event-Store-Streamed objects
    /// </summary>
    public abstract class DomainObject<TIdentifier>
        where TIdentifier : Identifier
    {
        /// <summary>
        ///     public Identifier for this Object
        /// </summary>
        public abstract TIdentifier Id { get; set; }

        /// <summary>
        ///     Current Version-Number of this Object
        /// </summary>
        public long CurrentVersion { get; set; } = -1;
    }
}
