using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Services.Stores
{
    /// <summary>
    ///     structured parameters for
    ///     <see cref="IEnvironmentProjectionStore.GetKeys(EnvironmentKeyQueryParameters)"  />
    ///     and
    ///     <see cref="IEnvironmentProjectionStore.GetKeyObjects(EnvironmentKeyQueryParameters)"  />
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
    }
}