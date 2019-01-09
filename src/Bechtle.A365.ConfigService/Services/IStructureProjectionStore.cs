using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    ///     read projected Structures
    /// </summary>
    public interface IStructureProjectionStore
    {
        /// <summary>
        ///     get a list of projected Structures
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<Result<IList<StructureIdentifier>>> GetAvailable(QueryRange range);

        /// <summary>
        ///     get a list of versions available for the given Structure
        /// </summary>
        /// <param name="name"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<Result<IList<int>>> GetAvailableVersions(string name, QueryRange range);

        /// <summary>
        ///     get the keys of a Structure
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<Result<IDictionary<string, string>>> GetKeys(StructureIdentifier identifier, QueryRange range);

        /// <summary>
        ///     get the variables of a Structure
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<Result<IDictionary<string, string>>> GetVariables(StructureIdentifier identifier, QueryRange range);
    }
}