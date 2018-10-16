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
        /// <returns></returns>
        Task<Result<IList<StructureIdentifier>>> GetAvailable();

        /// <summary>
        ///     get a list of versions available for the given Structure
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<Result<IList<int>>> GetAvailableVersions(string name);

        /// <summary>
        ///     get the keys of a Structure
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<Result<IDictionary<string, string>>> GetKeys(StructureIdentifier identifier);

        /// <summary>
        ///     get the variables of a Structure
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<Result<IDictionary<string, string>>> GetVariables(StructureIdentifier identifier);
    }
}