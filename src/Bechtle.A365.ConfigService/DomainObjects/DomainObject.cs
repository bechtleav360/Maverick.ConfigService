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
        public DateTime ChangedAt { get; init; }

        /// <summary>
        ///     Id of user that changed this object
        /// </summary>
        public string ChangedBy { get; init; }

        /// <summary>
        ///     Timestamp of when this object was created
        /// </summary>
        public DateTime CreatedAt { get; init; }

        /// <summary>
        ///     Id of user that created this object
        /// </summary>
        public string CreatedBy { get; init; }

        /// <summary>
        ///     Current Version-Number of this Object
        /// </summary>
        public long CurrentVersion { get; init; }

        /// <summary>
        ///     public Identifier for this Object
        /// </summary>
        public abstract TIdentifier Id { get; init; }

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

        /// <summary>
        ///     Copy all metadata from an existing DomainObject
        /// </summary>
        /// <param name="other"></param>
        protected DomainObject(DomainObject<TIdentifier> other)
        {
            ChangedAt = other.ChangedAt;
            ChangedBy = other.ChangedBy;
            CreatedAt = other.CreatedAt;
            CreatedBy = other.CreatedBy;
            CurrentVersion = other.CurrentVersion;
        }
    }
}
