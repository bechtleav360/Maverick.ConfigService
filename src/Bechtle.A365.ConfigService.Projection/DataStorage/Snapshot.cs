using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Projection.DataStorage
{
    /// <summary>
    ///     snapshot of configuration-data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Snapshot<T> where T : Identifier
    {
        /// <inheritdoc />
        public Snapshot(T identifier, int version, IDictionary<string, string> data)
        {
            Identifier = identifier;
            Version = version;
            Data = data;
        }

        /// <summary>
        ///     key-value pairs containing the actual snapshot-data
        /// </summary>
        public IDictionary<string, string> Data { get; }

        /// <summary>
        ///     <see cref="Identifier" /> of the associated data
        /// </summary>
        public T Identifier { get; }

        /// <summary>
        ///     version of the associated data
        /// </summary>
        public int Version { get; }
    }
}