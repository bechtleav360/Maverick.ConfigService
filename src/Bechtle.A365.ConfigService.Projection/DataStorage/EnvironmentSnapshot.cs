using System.Collections.Generic;
using System.Collections.ObjectModel;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Projection.DataStorage
{
    /// <summary>
    ///     snapshot of Environment-data
    /// </summary>
    public class EnvironmentSnapshot
    {
        /// <inheritdoc />
        public EnvironmentSnapshot(EnvironmentIdentifier identifier, IDictionary<string, string> data)
        {
            Identifier = identifier;
            Data = new ReadOnlyDictionary<string, string>(data ?? new Dictionary<string, string>());
        }

        /// <summary>
        ///     key-value pairs containing the actual snapshot-data
        /// </summary>
        public IDictionary<string, string> Data { get; }

        /// <summary>
        ///     <see cref="Identifier" /> of the associated data
        /// </summary>
        public EnvironmentIdentifier Identifier { get; }
    }
}