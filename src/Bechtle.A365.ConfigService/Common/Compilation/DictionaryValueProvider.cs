using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    /// <summary>
    ///     implementation of <see cref="IConfigValueProvider" /> using a Dictionary as backing-store
    /// </summary>
    public class DictionaryValueProvider : IConfigValueProvider
    {
        private readonly IDictionary<string, string> _repository;

        private readonly string _repositoryDisplayname;

        /// <inheritdoc cref="DictionaryValueProvider" />
        public DictionaryValueProvider(IDictionary<string, string> repository, string repositoryDisplayname)
        {
            // ensure the given dictionary is case-insensitive
            _repository = new Dictionary<string, string>(repository, StringComparer.OrdinalIgnoreCase);
            _repositoryDisplayname = repositoryDisplayname;
        }

        /// <summary>
        ///     compares two strings, and returns the number of characters that they share from the beginning.
        ///     (FooBarBaz, FooBarQue) => 6;
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        private static int MatchingStringLength(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return 0;

            var matching = 0;
            var shortestStringLength = Math.Min(left.Length, right.Length);

            for (var i = 0; i < shortestStringLength; ++i)
                if (left[i] == right[i])
                    ++matching;
                else
                    break;

            return matching;
        }

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

        /// <inheritdoc />
        public virtual Task<IResult<string>> TryGetValue(string path)
        {
            if (_repository.TryGetValue(path, out var result))
                return Task.FromResult(Result.Success(result));

            // search for a key that shares the most of its path with 'path'
            // that key might contain an indirection that needs to be resolved to resolve the original 'path'
            var possibleIndirections = _repository.Keys
                                                  .Select(k => (Key: k, MatchingLength: MatchingStringLength(k, path)))
                                                  .GroupBy(tuple => tuple.MatchingLength)
                                                  .OrderByDescending(g => g.Key)
                                                  .FirstOrDefault();

            // if we found multiple alternatives with equal amount of same path,
            // or we found no alternatives at all
            // we can't automatically resolve this indirection
            if (possibleIndirections?.Count() != 1)
                return Task.FromResult(Result.Error<string>($"path '{path}' could not be found in {_repositoryDisplayname}", ErrorCode.NotFound));

            return Task.FromResult(
                Result.Error(
                    $"path '{path}' could not be found in {_repositoryDisplayname}",
                    ErrorCode.NotFoundPossibleIndirection,
                    possibleIndirections.First().Key));
        }
    }
}
