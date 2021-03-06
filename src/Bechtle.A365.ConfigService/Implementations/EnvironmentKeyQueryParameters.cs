﻿using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     structured parameters for
    ///     <see cref="Interfaces.Stores.IEnvironmentProjectionStore.GetKeys" />
    ///     and
    ///     <see cref="Interfaces.Stores.IEnvironmentProjectionStore.GetKeyObjects(EnvironmentKeyQueryParameters)" />
    /// </summary>
    public struct EnvironmentKeyQueryParameters
    {
        /// <summary>
        ///     [Required] Environment-Id whose keys should be retrieved
        /// </summary>
        public EnvironmentIdentifier Environment { get; set; }

        /// <summary>
        ///     [Optional] Filter to apply to all Keys
        /// </summary>
        public string Filter { get; set; }

        /// <summary>
        ///     [Optional] If set and the Result-Set contains the given Value, only the Entry matching this will be returned.
        ///     This is used to disambiguate the result-set if a specific entry is searched for.
        /// </summary>
        public string PreferExactMatch { get; set; }

        /// <summary>
        ///     [Optional] Limit the Start and End of the Result-set. Used for Paging the Data
        /// </summary>
        public QueryRange Range { get; set; }

        /// <summary>
        ///     [Optional] Root-Path that should be removed from all found Entries
        /// </summary>
        public string RemoveRoot { get; set; }

        /// <summary>
        ///     [Optional] Max-Version to use when retrieving data
        /// </summary>
        public long TargetVersion { get; set; }
    }
}