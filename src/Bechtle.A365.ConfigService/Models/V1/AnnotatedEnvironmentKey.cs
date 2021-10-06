using System.Collections.Concurrent;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Models.V1
{
    /// <summary>
    ///     Environment-data annotated with a list of structures for each key
    /// </summary>
    public class AnnotatedEnvironmentKey
    {
        /// <summary>
        ///     Environment-Key
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        ///     List of Structures that used this Key
        /// </summary>
        public ConcurrentBag<StructureIdentifier> Structures { get; set; } = new();

        /// <summary>
        ///     Current Value
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
}