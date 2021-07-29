using System;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     base-class for all Event-Store-Streamed objects
    /// </summary>
    public abstract class DomainObject<TIdentifier>
        where TIdentifier : Identifier
    {
        /// <summary>
        ///     Timestamp of when this object was last changed
        /// </summary>
        public DateTime ChangedAt { get; set; }

        /// <summary>
        ///     Id of user that changed this object
        /// </summary>
        public string ChangedBy { get; set; }

        /// <summary>
        ///     Timestamp of when this object was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        ///     Id of user that created this object
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        ///     Current Version-Number of this Object
        /// </summary>
        public long CurrentVersion { get; set; }

        /// <summary>
        ///     public Identifier for this Object
        /// </summary>
        public abstract TIdentifier Id { get; set; }

        /// <summary>
        ///     Initializes all Properties to known-invalid values
        /// </summary>
        protected DomainObject()
        {
            ChangedAt = DateTime.UnixEpoch;
            ChangedBy = "Anonymous";
            CreatedAt = DateTime.UnixEpoch;
            CreatedBy = "Anonymous";
            CurrentVersion = -1;
        }
    }
}
