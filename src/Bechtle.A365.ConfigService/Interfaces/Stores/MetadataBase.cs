using System;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     Base-Information regarding DomainObjects
    /// </summary>
    public abstract class MetadataBase
    {
        /// <summary>
        ///     UTC-Timestamp for when the Object was Created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        ///     Id of the User that created the Object
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        ///     UTC-Timestamp for then the object was last modified
        /// </summary>
        public DateTime ChangedAt { get; set; }

        /// <summary>
        ///     Id of the User that last changed the Object
        /// </summary>
        public string ChangedBy { get; set; }
    }
}