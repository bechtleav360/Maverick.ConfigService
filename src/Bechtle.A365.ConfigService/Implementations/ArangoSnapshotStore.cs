using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     Snapshot-Store using ArangoDBs Document-Store as backend
    /// </summary>
    public class ArangoSnapshotStore : ISnapshotStore
    {
        private const string ConfigBasePath = "SnapshotConfiguration:Stores:Arango";
        private readonly IConfiguration _configuration;

        private readonly ArangoHttpClient _httpClient;
        private readonly ILogger<ArangoSnapshotStore> _logger;
        private bool _collectionCreated;

        /// <inheritdoc />
        public ArangoSnapshotStore(ArangoHttpClient httpClient,
                                   IConfiguration configuration,
                                   ILogger<ArangoSnapshotStore> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<IResult<long>> GetLatestSnapshotNumbers() => Task.FromResult(Result.Success(0L));

        /// <inheritdoc />
        public Task<IResult<DomainObjectSnapshot>> GetSnapshot<T>(string identifier) where T : DomainObject
            => GetSnapshotInternal<T>(identifier, long.MaxValue);

        /// <inheritdoc />
        public Task<IResult<DomainObjectSnapshot>> GetSnapshot<T>(string identifier, long maxVersion) where T : DomainObject
            => GetSnapshotInternal<T>(identifier, maxVersion);

        /// <inheritdoc />
        public Task<IResult<DomainObjectSnapshot>> GetSnapshot(string dataType, string identifier)
            => GetSnapshotInternal(dataType, identifier, long.MaxValue);

        /// <inheritdoc />
        public Task<IResult<DomainObjectSnapshot>> GetSnapshot(string dataType, string identifier, long maxVersion)
            => GetSnapshotInternal(dataType, identifier, maxVersion);

        /// <inheritdoc />
        public async Task<IResult> SaveSnapshots(IList<DomainObjectSnapshot> snapshots)
        {
            if (!TryGetCollectionName(out var collection))
                return Result.Error("arango-collection is undefined", ErrorCode.DbQueryError);

            await EnsureCollectionCreated();

            var json = JsonSerializer.Serialize(snapshots);

            var response = await _httpClient.PostAsync($"_api/document/{collection}", new StringContent(json, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"couldn't save snapshots to arango; {response.StatusCode:D}-{response.StatusCode:G} {response.ReasonPhrase}");
                return Result.Error($"arango-collection ({collection}) could not be updated " +
                                    $"{response.StatusCode:D}-{response.StatusCode:G} {response.ReasonPhrase}",
                                    ErrorCode.DbQueryError);
            }

            try
            {
                await using var responseStream = await response.Content.ReadAsStreamAsync();
                var responseObject = await JsonSerializer.DeserializeAsync<JsonElement>(responseStream);

                if (responseObject.TryGetProperty("error", out var error) && !error.GetBoolean())
                {
                    _logger.LogDebug("arango-response seems to be an error");

                    responseObject.TryGetProperty("errorMessage", out var errorMessage);
                    responseObject.TryGetProperty("errorNum", out var errorNum);
                    responseObject.TryGetProperty("code", out var code);

                    return Result.Error($"arango-collection ({collection}) couldn't be updated: " +
                                        $"Code='{code}'; ErrorNum='{errorNum}'; ErrorMessage='{errorMessage}'",
                                        ErrorCode.DbUpdateError);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "couldn't read response from arango");
                return Result.Error("couldn't read response from arango", ErrorCode.DbUpdateError);
            }

            return Result.Success();
        }

        /// <summary>
        ///     ask arango if the configured collection exists or not
        /// </summary>
        /// <returns></returns>
        private async Task<bool> CollectionExists()
        {
            if (!TryGetCollectionName(out var collection))
                return false;

            var response = await _httpClient.GetAsync($"_api/collection/{collection}");

            if (response.IsSuccessStatusCode)
                return true;

            _logger.LogWarning($"couldn't get infos about collection '{collection}' from arango; " +
                               $"{response.StatusCode:D}-{response.StatusCode:G} {response.ReasonPhrase}");
            return false;
        }

        /// <summary>
        ///     ensure the configured collection is created
        /// </summary>
        /// <returns></returns>
        private async Task EnsureCollectionCreated()
        {
            if (_collectionCreated)
                return;

            if (!_configuration.GetSection($"{ConfigBasePath}:CreateCollection").Get<bool>())
                return;

            if (await CollectionExists())
            {
                // in case someone else created the collection for us
                _collectionCreated = true;
                return;
            }

            try
            {
                var collectionDefinition = _configuration.GetSection($"{ConfigBasePath}:Collection").Get<JsonElement>().ToString();

                if (_logger.IsEnabled(LogLevel.Information))
                    _logger.LogInformation($"autoCreating arango-collection: {collectionDefinition}");

                var response = await _httpClient.PostAsync("_api/collection",
                                                           new StringContent(collectionDefinition,
                                                                             Encoding.UTF8,
                                                                             "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    // in case we created the collection ourselves
                    _collectionCreated = true;
                    _logger.LogInformation("collection successfully created");
                    return;
                }

                await using var responseStream = await response.Content.ReadAsStreamAsync();
                var responseObject = await JsonSerializer.DeserializeAsync<JsonElement>(responseStream);

                if (responseObject.TryGetProperty("error", out var error) && !error.GetBoolean())
                {
                    _logger.LogDebug("arango-response seems to be an error");

                    responseObject.TryGetProperty("errorMessage", out var errorMessage);
                    responseObject.TryGetProperty("errorNum", out var errorNum);
                    responseObject.TryGetProperty("code", out var code);

                    _logger.LogInformation($"arango-collection couldn't be created: Code='{code}'; ErrorNum='{errorNum}'; ErrorMessage='{errorMessage}'");
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "couldn't read response from arango");
            }
        }

        private Task<IResult<DomainObjectSnapshot>> GetSnapshotInternal<T>(string identifier, long maxVersion)
            => GetSnapshotInternal(typeof(T).Name, identifier, maxVersion);

        private Task<IResult<DomainObjectSnapshot>> GetSnapshotInternal(string dataType, string identifier, long maxVersion)
            => Task.FromResult(Result.Error<DomainObjectSnapshot>(string.Empty, ErrorCode.Undefined));

        /// <summary>
        ///     try to get the Collection.Name from this Stores Configuration
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        private bool TryGetCollectionName(out string collection)
        {
            collection = _configuration.GetSection($"{ConfigBasePath}:Collection:Name").Get<string>();

            if (!string.IsNullOrWhiteSpace(collection))
                return true;

            _logger.LogWarning($"arango-collection is unset ({ConfigBasePath}:Collection:Name)");
            return false;
        }
    }
}