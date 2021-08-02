using System;
using System.IO;
using Bechtle.A365.ConfigService.Interfaces;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     Implementation of <see cref="IDomainObjectManager"/> that points to './data/projections.db'
    /// </summary>
    public class DomainObjectStoreLocationProvider : IDomainObjectStoreLocationProvider
    {
        private readonly FileInfo _localDbLocation = new FileInfo(Path.Combine(Environment.CurrentDirectory, "data/projections.current.db"));

        /// <inheritdoc />
        public string FileName => _localDbLocation.FullName;
    }
}
