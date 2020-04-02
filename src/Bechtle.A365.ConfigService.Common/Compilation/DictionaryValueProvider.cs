using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    /// <summary>
    ///     implementation of <see cref="IConfigValueProvider"/> using a Dictionary as backing-store
    /// </summary>
    public class DictionaryValueProvider : IConfigValueProvider
    {
        private readonly IDictionary<string, string> _repository;

        private readonly string _repositoryDisplayname;

        /// <inheritdoc cref="DictionaryValueProvider"/>
        public DictionaryValueProvider(IDictionary<string, string> repository, string repositoryDisplayname)
        {
            _repository = repository;
            _repositoryDisplayname = repositoryDisplayname;
        }

        /// <inheritdoc />
        public virtual Task<IResult<string>> TryGetValue(string path)
            => Task.FromResult(
                _repository.TryGetValue(path, out var result)
                    ? Result.Success(result)
                    : Result.Error<string>($"path '{path}' could not be found in {_repositoryDisplayname}", ErrorCode.NotFound));

        /// <inheritdoc />
        public virtual Task<IResult<Dictionary<string, string>>> TryGetRange(string query)
        {
            var sanitizedQuery = query.TrimEnd('*');

            // select all pairs that start with 'query'
            // remove the 'query' part from each result
            // return as new dictionary
            var results = _repository.Where(kvp => kvp.Key.StartsWith(sanitizedQuery, StringComparison.OrdinalIgnoreCase))
                                     .Select(kvp => new KeyValuePair<string, string>(kvp.Key.Substring(sanitizedQuery.Length), kvp.Value))
                                     .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return Task.FromResult(Result.Success(results));
        }
    }
}