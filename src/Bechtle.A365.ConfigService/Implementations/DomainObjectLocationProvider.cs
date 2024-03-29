﻿using System.IO;
using Bechtle.A365.ConfigService.Interfaces;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     Implementation of <see cref="IDomainObjectManager" /> that points to './data/cache/projections.db'
    /// </summary>
    public class DomainObjectStoreLocationProvider : IDomainObjectStoreLocationProvider
    {
        private readonly FileInfo _localDbLocation = new("./data/cache/projections.current.db");

        /// <inheritdoc />
        public string? Directory => _localDbLocation.DirectoryName;

        /// <inheritdoc />
        public string FileName => _localDbLocation.FullName;
    }
}
