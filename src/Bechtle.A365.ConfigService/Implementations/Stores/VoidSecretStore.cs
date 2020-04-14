using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <summary>
    ///     implementation of <see cref="ISecretConfigValueProvider"/> that doesn't return values (always errors, always returns empty)
    /// </summary>
    public class VoidSecretStore : ISecretConfigValueProvider
    {
        /// <inheritdoc />
        public Task<IResult<string>> TryGetValue(string path)
            => Task.FromResult(Result.Error<string>("no valid Secret-Store registered", ErrorCode.DbQueryError));

        /// <inheritdoc />
        public Task<IResult<Dictionary<string, string>>> TryGetRange(string query)
            => Task.FromResult(Result.Error<Dictionary<string, string>>("no valid Secret-Store registered", ErrorCode.DbQueryError));
    }
}