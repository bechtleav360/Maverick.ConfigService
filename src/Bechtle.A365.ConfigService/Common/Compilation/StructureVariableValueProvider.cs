using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    /// <summary>
    ///     Component that resolves references pointing to the Variables of a Structure
    /// </summary>
    public class StructureVariableValueProvider : DictionaryValueProvider
    {
        /// <inheritdoc />
        public StructureVariableValueProvider(IDictionary<string, string?> repository) : base(repository, "structure-variables")
        {
        }

        /// <inheritdoc />
        public override Task<IResult<Dictionary<string, string?>>> TryGetRange(string query)
            => Task.FromResult(
                Result.Error<Dictionary<string, string?>>(
                    "range-querying structure-variables is not supported",
                    ErrorCode.DbQueryError));
    }
}
