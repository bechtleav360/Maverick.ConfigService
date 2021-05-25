using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    public class StructureVariableValueProvider : DictionaryValueProvider
    {
        /// <inheritdoc />
        public StructureVariableValueProvider(IDictionary<string, string> repository) : base(repository, "structure-variables")
        {
        }

        /// <inheritdoc />
        public override Task<IResult<Dictionary<string, string>>> TryGetRange(string query)
            => Task.FromResult(
                Result.Error<Dictionary<string, string>>(
                    "range-querying structure-variables is not supported",
                    ErrorCode.DbQueryError));
    }
}