using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Models.V1;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     read projected Structures
    /// </summary>
    public interface IStructureProjectionStore : IDisposable, IAsyncDisposable
    {
        /// <summary>
        ///     create a new Structure with the given Identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="keys"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        Task<IResult> Create(
            StructureIdentifier identifier,
            IDictionary<string, string?> keys,
            IDictionary<string, string?> variables);

        /// <summary>
        ///     delete a set of variables from the current Structure
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="variablesToDelete"></param>
        /// <returns></returns>
        Task<IResult> DeleteVariables(StructureIdentifier identifier, ICollection<string> variablesToDelete);

        /// <summary>
        ///     get a list of projected Structures
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<IResult<Page<StructureIdentifier>>> GetAvailable(QueryRange range);

        /// <summary>
        ///     get a list of versions available for the given Structure
        /// </summary>
        /// <param name="name"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<IResult<Page<int>>> GetAvailableVersions(string name, QueryRange range);

        /// <summary>
        ///     get the keys of a Structure
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<IResult<Page<KeyValuePair<string, string?>>>> GetKeys(StructureIdentifier identifier, QueryRange range);

        /// <summary>
        ///     get metadata for a Structure
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult<ConfigStructureMetadata>> GetMetadata(StructureIdentifier identifier);

        /// <summary>
        ///     get metadata for a Structure
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<IResult<Page<ConfigStructureMetadata>>> GetMetadata(QueryRange range);

        /// <summary>
        ///     get the variables of a Structure
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<IResult<Page<KeyValuePair<string, string?>>>> GetVariables(StructureIdentifier identifier, QueryRange range);

        /// <summary>
        ///     Update / Set a set of Variables to the given values for the given Structure
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        Task<IResult> UpdateVariables(StructureIdentifier identifier, IDictionary<string, string> variables);
    }
}
