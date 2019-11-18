using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Converters;
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

        private readonly HttpClient _httpClient;
        private readonly ILogger<ArangoSnapshotStore> _logger;
        private readonly IJsonTranslator _translator;
        private bool _collectionCreated;

        /// <inheritdoc />
        public ArangoSnapshotStore(IHttpClientFactory factory,
                                   IConfiguration configuration,
                                   IJsonTranslator translator,
                                   ILogger<ArangoSnapshotStore> logger)
        {
            _httpClient = factory.CreateClient("Arango");
            _configuration = configuration;
            _translator = translator;
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

            var groupNum = 1;
            const int groupSize = 8;
            var i = 0;

            _logger.LogDebug($"separating '{snapshots.Count}' snapshots into batches of up to '{groupSize}' items");

            var groups = snapshots.GroupBy(_ =>
            {
                if (i++ <= groupSize)
                    return groupNum;

                i = 0;
                ++groupNum;
                return groupNum;
            }).ToList();

            _logger.LogDebug($"separated '{snapshots.Count}' snapshots into '{groupNum}' batches of up to '{groupSize}' items");

            foreach (var group in groups)
            {
                _logger.LogDebug($"saving snapshot-group {group.Key} with {group.Count()} items");
                var result = await SaveSnapshotsInternal(group.ToList(), collection);
                if (result.IsError)
                    return result;
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

            try
            {
                var response = await _httpClient.GetAsync($"_api/collection/{collection}");

                if (response.IsSuccessStatusCode)
                    return true;

                _logger.LogWarning($"couldn't get infos about collection '{collection}' from arango; " +
                                   $"{response.StatusCode:D}-{response.StatusCode:G} {response.ReasonPhrase}");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"couldn't determine if collection '{collection}' exists");
            }

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
                var collectionDefinition = SectionToJson($"{ConfigBasePath}:Collection").ToString();

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

                var jsonDoc = await JsonDocument.ParseAsync(responseStream);
                var element = jsonDoc.RootElement;

                if (element.TryGetProperty("error", out var error) && error.GetBoolean())
                {
                    _logger.LogDebug("arango-response seems to be an error");

                    element.TryGetProperty("errorMessage", out var errorMessage);
                    element.TryGetProperty("errorNum", out var errorNum);
                    element.TryGetProperty("code", out var code);

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

        private async Task<IResult> SaveSnapshotsInternal(IList<DomainObjectSnapshot> snapshots, string collection)
        {
            var metaVersion = snapshots.Max(s => s.Version);

            var json = JsonSerializer.Serialize(snapshots.Select(s => new ArangoSnapshot
            {
                Key = Convert.ToBase64String(Encoding.UTF8.GetBytes(s.Identifier)),
                Data = s,
                Version = s.Version,
                DataType = s.DataType,
                Identifier = s.Identifier,
                MetaVersion = metaVersion
            }).ToList());

            var response = await _httpClient.PostAsync($"_api/document/{collection}", new StringContent(json, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
                return Result.Success();

            _logger.LogWarning($"couldn't save snapshots to arango; {response.StatusCode:D}-{response.StatusCode:G} {response.ReasonPhrase}");
            return Result.Error($"arango-collection ({collection}) could not be updated " +
                                $"{response.StatusCode:D}-{response.StatusCode:G} {response.ReasonPhrase}",
                                ErrorCode.DbQueryError);
        }

        private JsonElement SectionToJson(string basePath)
        {
            var items = new Dictionary<string, string>();
            var stack = new Stack<IConfigurationSection>(_configuration.GetSection(basePath).GetChildren());
            while (stack.TryPop(out var item))
            {
                items[item.Path.Substring(basePath.Length + 1)] = item.Value;
                foreach (var child in item.GetChildren())
                    stack.Push(child);
            }

            return _translator.ToJson(items, ":");
        }

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

        private class ArangoSnapshot
        {
            [JsonPropertyName("_key")]
            public string Key { get; set; }

            public string DataType { get; set; }

            public string Identifier { get; set; }

            public DomainObjectSnapshot Data { get; set; }

            public long MetaVersion { get; set; }

            public long Version { get; set; }
        }
    }
}