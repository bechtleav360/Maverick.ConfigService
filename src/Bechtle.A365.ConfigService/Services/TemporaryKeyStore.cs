using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Services
{
    /// <inheritdoc cref="ITemporaryKeyStore" />
    /// <summary>
    ///     implementation of <see cref="ITemporaryKeyStore"/>, stores keys using a <see cref="IDistributedCache"/>.
    ///     keeps a journal of stored keys / structure, to make <see cref="GetAll()"/> and overloads possible.
    /// </summary>
    public class TemporaryKeyStore : ITemporaryKeyStore
    {
        private const string CacheKeyJournalName = CacheRegion + "Journal";
        private const string CacheRegion = "A365.ConfigService.TemporaryStore.";
        private readonly IDistributedCache _cache;
        private readonly ILogger _logger;

        /// <inheritdoc />
        public TemporaryKeyStore(ILogger<ITemporaryKeyStore> logger, IDistributedCache cache)
        {
            _logger = logger;
            _cache = cache;
        }

        /// <inheritdoc />
        public Task<IResult> Extend(string structure, string key) => Extend(structure, new[] {key});

        /// <inheritdoc />
        public async Task<IResult> Extend(string structure, IEnumerable<string> keys)
        {
            var keyList = keys.ToList();

            try
            {
                var tasks = keyList.AsParallel()
                                   .Select(k => _cache.RefreshAsync(MakeCacheKey(structure, k)))
                                   .ToList();

                await Task.WhenAll(tasks);

                return Result.Success();
            }
            catch (Exception e)
            {
                var message = $"could not extend the lifespan of one of the following keys:\r\n{string.Join("; ", keyList)}";
                _logger.LogWarning(e, message);
                return Result.Error(message, ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public Task<IResult> Extend(string structure, string key, TimeSpan duration) => Extend(structure, new[] {key}, duration);

        /// <inheritdoc />
        public async Task<IResult> Extend(string structure, IEnumerable<string> keys, TimeSpan duration)
        {
            var keyList = keys.ToList();

            try
            {
                var values = await Get(structure, keyList);

                if (values.IsError)
                    return values;

                var tasks = values.Data
                                  .AsParallel()
                                  .Select(kvp => Set(structure, kvp.Key, kvp.Value, duration))
                                  .ToList();

                await Task.WhenAll(tasks);

                var failures = tasks.Where(t => t.Result.IsError)
                                    .ToList();

                _logger.LogDebug($"setting the lifespan of '{keyList.Count}' items to '{duration:g}'; " +
                                 $"'{failures.Count}' failed, {keyList.Count - failures.Count} succeeded");

                return failures.Any()
                           ? Result.Success()
                           : Result.Error("not all keys could be refreshed with the new duration", ErrorCode.DbUpdateError);
            }
            catch (Exception e)
            {
                var message = $"could not extend the lifespan of one of the following keys:\r\n{string.Join("; ", keyList)}";
                _logger.LogWarning(e, message);
                return Result.Error(message, ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IDictionary<string, string>>> Get(string structure, IEnumerable<string> keys)
        {
            var keyList = keys.ToList();

            try
            {
                var tasks = keyList.AsParallel()
                                   .Select(k => (Key: k, Task: _cache.GetAsync(MakeCacheKey(structure, k))))
                                   .ToList();

                await Task.WhenAll(tasks.Select(t => t.Task));

                IDictionary<string, string> result = tasks.ToDictionary(t => t.Key, t => Encoding.UTF8.GetString(t.Task?.Result ?? new byte[0]));

                return Result.Success(result);
            }
            catch (Exception e)
            {
                var message = $"could not retrieve values for temporary keys: {string.Join(", ", keyList)}";

                _logger.LogWarning(e, message);
                return Result.Error<IDictionary<string, string>>(message, ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<string>> Get(string structure, string key)
        {
            var result = await Get(structure, new[] {key});

            return result.IsError
                       ? Result.Error<string>(result.Message, result.Code)
                       : Result.Success(result.Data.Values.First());
        }

        /// <inheritdoc />
        public async Task<IResult<IDictionary<string, IDictionary<string, string>>>> GetAll()
        {
            var journal = await GetJournal();

            IDictionary<string, IDictionary<string, string>> result = new Dictionary<string, IDictionary<string, string>>();

            foreach (var (structure, keys) in journal)
            {
                var values = await Get(structure, keys);

                if (values.IsError)
                    continue;

                result.Add(structure, values.Data);
            }

            return Result.Success(result);
        }

        /// <inheritdoc />
        public async Task<IResult<IDictionary<string, string>>> GetAll(string structure)
        {
            var journal = await GetJournal();

            if (!journal.ContainsKey(structure))
                return Result.Error<IDictionary<string, string>>($"no keys for '{structure}' in journal - try retrieving keys directly",
                                                                 ErrorCode.DbQueryError);

            var values = await Get(structure, journal[structure]);

            if (values.IsError)
                return Result.Error<IDictionary<string, string>>("", ErrorCode.DbQueryError);

            return Result.Success(values.Data);
        }

        /// <inheritdoc />
        public Task<IResult> Remove(string structure, string key) => Remove(structure, new[] {key});

        /// <inheritdoc />
        public async Task<IResult> Remove(string structure, IEnumerable<string> keys)
        {
            var keyList = keys.ToList();

            try
            {
                var tasks = keyList.AsParallel()
                                   .Select(k => _cache.RemoveAsync(MakeCacheKey(structure, k)))
                                   .ToList();

                await Task.WhenAll(tasks);

                await RemoveFromJournal(structure, keyList.ToArray());

                return Result.Success();
            }
            catch (Exception e)
            {
                var message = $"could not remove one of the following keys:\r\n{string.Join("; ", keyList)}";
                _logger.LogWarning(e, message);
                return Result.Error(message, ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public Task<IResult> Set(string structure, string key, string value, TimeSpan duration)
            => Set(structure, new Dictionary<string, string> {{key, value}}, duration);

        /// <inheritdoc />
        public async Task<IResult> Set(string structure, IDictionary<string, string> values, TimeSpan duration)
        {
            try
            {
                var byteValues = values.ToDictionary(kvp => kvp.Value,
                                                     kvp => Encoding.UTF8.GetBytes(kvp.Value));

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    SlidingExpiration = duration
                };

                var tasks = byteValues.AsParallel()
                                      .Select(kvp => _cache.SetAsync(MakeCacheKey(structure, kvp.Key), kvp.Value, cacheOptions))
                                      .ToList();

                await Task.WhenAll(tasks);

                await AddToJournal(structure, values.Keys.ToArray());

                return Result.Success();
            }
            catch (Exception e)
            {
                var message = $"could not store value for one or more temporary keys in: {string.Join(", ", values.Keys)}";
                _logger.LogWarning(e, message);
                return Result.Error(message, ErrorCode.DbUpdateError);
            }
        }

        /// <summary>
        ///     add keys to the internal journal
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        private async Task AddToJournal(string structure, params string[] keys)
        {
            var currentJournal = await GetJournal();

            foreach (var key in keys)
            {
                if (!currentJournal.ContainsKey(structure))
                    currentJournal.Add(structure, new List<string>());

                if (!currentJournal[structure].Contains(key))
                    currentJournal[structure].Add(key);
            }

            await StoreJournal(currentJournal);
        }

        /// <summary>
        ///     retrieve a copy of the journal
        /// </summary>
        /// <returns></returns>
        private async Task<Dictionary<string, List<string>>> GetJournal()
        {
            var returnValue = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var currentJournalBytes = await _cache.GetAsync(CacheKeyJournalName);

                if (currentJournalBytes.Any())
                {
                    var currentJournalValue = Encoding.UTF8.GetString(currentJournalBytes);

                    if (!string.IsNullOrWhiteSpace(currentJournalValue))
                    {
                        var value = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(currentJournalValue);

                        foreach (var (key, values) in value)
                            returnValue.Add(key, values);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "could not retrieve journal for temporary keys");
            }

            return returnValue;
        }

        /// <summary>
        ///     make a CacheKey taking all necessary information into account
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private string MakeCacheKey(string structure, string key) => $"{CacheRegion}.{structure}.{key}".ToLowerInvariant();

        /// <summary>
        ///     remove keys from the internal journal
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        private async Task RemoveFromJournal(string structure, params string[] keys)
        {
            var currentJournal = await GetJournal();

            foreach (var key in keys)
                if (currentJournal.ContainsKey(structure) && currentJournal[structure].Contains(key))
                    currentJournal[structure].Remove(key);

            await StoreJournal(currentJournal);
        }

        /// <summary>
        ///     store the given object as the new public journal
        /// </summary>
        /// <param name="journal"></param>
        /// <returns></returns>
        private async Task StoreJournal(object journal)
        {
            try
            {
                await _cache.SetAsync(CacheKeyJournalName, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(journal, Formatting.None)));
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "could not store journal for temporary keys");
            }
        }
    }
}