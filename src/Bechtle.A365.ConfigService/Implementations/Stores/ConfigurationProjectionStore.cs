using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Models.V1;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <inheritdoc />
    public sealed class ConfigurationProjectionStore : IConfigurationProjectionStore
    {
        private readonly IDomainObjectManager _domainObjectManager;
        private readonly ILogger<ConfigurationProjectionStore> _logger;

        /// <inheritdoc cref="ConfigurationProjectionStore" />
        public ConfigurationProjectionStore(
            ILogger<ConfigurationProjectionStore> logger,
            IDomainObjectManager domainObjectManager)
        {
            _logger = logger;
            _domainObjectManager = domainObjectManager;
        }

        /// <inheritdoc />
        public async Task<IResult> Build(ConfigurationIdentifier identifier, DateTime? validFrom, DateTime? validTo)
        {
            _logger.LogDebug(
                "building new configuration '{Identifier}', from={ValidFrom}, to={ValidTo}",
                identifier,
                validFrom,
                validTo);

            return await _domainObjectManager.CreateConfiguration(identifier, validFrom, validTo, CancellationToken.None);
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync() => new(Task.CompletedTask);

        /// <inheritdoc />
        public async Task<IResult<Page<ConfigurationIdentifier>>> GetAvailable(DateTime when, QueryRange range)
        {
            _logger.LogDebug(
                "collecting available configurations at '{When:O}', range={Range}",
                when,
                range);

            return await _domainObjectManager.GetConfigurations(range, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<IResult<Page<ConfigurationIdentifier>>> GetAvailableWithEnvironment(
            EnvironmentIdentifier environment,
            DateTime when,
            QueryRange range)
        {
            _logger.LogDebug("collecting available configurations with {Identifier} at '{When}', range={Range}", environment, when, range);

            return await _domainObjectManager.GetConfigurations(environment, range, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<IResult<Page<ConfigurationIdentifier>>> GetAvailableWithStructure(
            StructureIdentifier structure,
            DateTime when,
            QueryRange range)
        {
            _logger.LogDebug("collecting available configurations with {Identifier} at '{When}', range={Range}", structure, when, range);

            return await _domainObjectManager.GetConfigurations(structure, range, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<IResult<JsonElement>> GetJson(ConfigurationIdentifier identifier, DateTime when)
        {
            var formattedParams = "("
                                  + $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Category)}: {identifier.Environment.Category}; "
                                  + $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Name)}: {identifier.Environment.Name}; "
                                  + $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Name)}: {identifier.Structure.Name}; "
                                  + $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Version)}: {identifier.Structure.Version}"
                                  + ")";

            try
            {
                _logger.LogDebug("getting json of '{Identifier}' at {When}", identifier, when);

                IResult<PreparedConfiguration> configuration = await _domainObjectManager.GetConfiguration(identifier, CancellationToken.None);
                if (configuration.IsError)
                    return Result.Error<JsonElement>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                return Result.Success(JsonDocument.Parse(configuration.CheckedData.Json).RootElement);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configuration keys for id: {Identifier}", identifier);
                return Result.Error<JsonElement>(
                    $"failed to retrieve projected configuration keys for id: {formattedParams}",
                    ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<Page<KeyValuePair<string, string?>>>> GetKeys(
            ConfigurationIdentifier identifier,
            DateTime when,
            QueryRange range)
        {
            var formattedParams = "("
                                  + $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Category)}: {identifier.Environment.Category}; "
                                  + $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Name)}: {identifier.Environment.Name}; "
                                  + $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Name)}: {identifier.Structure.Name}; "
                                  + $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Version)}: {identifier.Structure.Version}"
                                  + ")";

            try
            {
                _logger.LogDebug("getting keys of '{Identifier}' at {When}", identifier, when);

                IResult<PreparedConfiguration> configuration = await _domainObjectManager.GetConfiguration(identifier, CancellationToken.None);
                if (configuration.IsError)
                    return Result.Error<Page<KeyValuePair<string, string?>>>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                List<KeyValuePair<string, string?>> items =
                    configuration.CheckedData
                                 .Keys
                                 .OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase)
                                 .Skip(range.Offset)
                                 .Take(range.Length)
                                 .ToList();

                var page = new Page<KeyValuePair<string, string?>>
                {
                    Items = items,
                    Count = items.Count,
                    Offset = range.Offset,
                    TotalCount = configuration.CheckedData.Keys.Count,
                };

                return Result.Success(page);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configuration keys for id: {Identifier}", identifier);
                return Result.Error<Page<KeyValuePair<string, string?>>>(
                    $"failed to retrieve projected configuration keys for id: {formattedParams}",
                    ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<Page<ConfigurationIdentifier>>> GetStale(QueryRange range)
        {
            try
            {
                _logger.LogDebug("retrieving stale configurations, range={Range}", range);
                return await _domainObjectManager.GetStaleConfigurations(range, CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configurations");
                return Result.Error<Page<ConfigurationIdentifier>>("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<Page<string>>> GetUsedConfigurationKeys(
            ConfigurationIdentifier identifier,
            DateTime when,
            QueryRange range)
        {
            var formattedParams = "("
                                  + $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Category)}: {identifier.Environment.Category}; "
                                  + $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Name)}: {identifier.Environment.Name}; "
                                  + $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Name)}: {identifier.Structure.Name}; "
                                  + $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Version)}: {identifier.Structure.Version}"
                                  + ")";

            try
            {
                _logger.LogDebug("retrieving used Env-Keys for configuration {Identifier}", identifier);

                IResult<PreparedConfiguration> configuration = await _domainObjectManager.GetConfiguration(identifier, CancellationToken.None);
                if (configuration.IsError)
                    return Result.Error<Page<string>>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                var result = configuration.CheckedData
                                          .UsedKeys
                                          .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                                          .Skip(range.Offset)
                                          .Take(range.Length)
                                          .ToList();

                _logger.LogDebug("collected '{UsedKeys}' keys", result.Count);

                var page = new Page<string>
                {
                    Items = result,
                    Count = result.Count,
                    Offset = range.Offset,
                    TotalCount = configuration.CheckedData.UsedKeys.Count,
                };

                return Result.Success(page);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve used environment keys for id: {Identifier}", identifier);
                return Result.Error<Page<string>>(
                    $"failed to retrieve used environment keys for id: {formattedParams}: {e}",
                    ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<string>> GetVersion(ConfigurationIdentifier identifier, DateTime when)
        {
            var formattedParams = "("
                                  + $"Environment.Category: {identifier.Environment.Category}; "
                                  + $"Environment.Name: {identifier.Environment.Name}; "
                                  + $"Structure.Name: {identifier.Structure.Name}; "
                                  + $"Structure.Version: {identifier.Structure.Version}"
                                  + ")";

            try
            {
                _logger.LogDebug("retrieving Config-Version of '{Identifier}' at {When}", identifier, when);

                IResult<PreparedConfiguration> configuration = await _domainObjectManager.GetConfiguration(identifier, CancellationToken.None);
                if (configuration.IsError)
                    return Result.Error<string>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                var result = configuration.CheckedData.ConfigurationVersion.ToString();

                _logger.LogDebug("Config-Version of {Identifier} = {Version}", identifier, result);

                return Result.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve used environment keys for id: {Configuration}", identifier);
                return Result.Error<string>(
                    $"failed to retrieve used environment keys for id: {formattedParams}: {e}",
                    ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<bool>> IsStale(ConfigurationIdentifier identifier)
        {
            try
            {
                return await _domainObjectManager.IsStale(identifier);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configurations");
                return Result.Error<bool>("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<PreparedConfigurationMetadata>> GetMetadata(ConfigurationIdentifier identifier)
        {
            try
            {
                _logger.LogDebug("retrieving config-metadata of '{Identifier}'", identifier);

                IResult<PreparedConfiguration> result = await _domainObjectManager.GetConfiguration(identifier, CancellationToken.None);

                if (result.IsError)
                    return Result.Error<PreparedConfigurationMetadata>(result.Message, result.Code);

                PreparedConfiguration configuration = result.CheckedData;

                var metadata = new PreparedConfigurationMetadata(configuration.Id)
                {
                    ChangedAt = configuration.ChangedAt,
                    ChangedBy = configuration.ChangedBy,
                    CreatedAt = configuration.CreatedAt,
                    CreatedBy = configuration.CreatedBy,
                    KeyCount = configuration.Keys.Count,
                    Warnings = configuration.Warnings,
                    WarningCount = configuration.Warnings.Count,
                    Errors = configuration.Errors,
                    ErrorCount = configuration.Errors.Count,
                    UsedKeys = configuration.UsedKeys,
                    UsedKeysCount = configuration.UsedKeys.Count
                };

                return Result.Success(metadata);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configurations");
                return Result.Error<PreparedConfigurationMetadata>("failed to retrieve projected configuration", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<Page<PreparedConfigurationMetadata>>> GetMetadata(QueryRange range)
        {
            try
            {
                _logger.LogDebug("retrieving config-metadata for range '{Range}'", range);

                var ids = await _domainObjectManager.GetConfigurations(range, CancellationToken.None);
                if (ids.IsError)
                    return Result.Error<Page<PreparedConfigurationMetadata>>(ids.Message, ids.Code);

                var results = new List<PreparedConfigurationMetadata>();
                foreach (var configId in ids.CheckedData.Items)
                {
                    IResult<PreparedConfigurationMetadata> result = await GetMetadata(configId);
                    if (result.IsError)
                        return Result.Error<Page<PreparedConfigurationMetadata>>(result.Message, result.Code);
                    results.Add(result.CheckedData);
                }

                return Result.Success(
                    new Page<PreparedConfigurationMetadata>
                    {
                        Items = results,
                        Count = results.Count,
                        Offset = range.Offset,
                        TotalCount = ids.CheckedData.TotalCount
                    });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configurations");
                return Result.Error<Page<PreparedConfigurationMetadata>>("failed to retrieve projected configuration", ErrorCode.DbQueryError);
            }
        }
    }
}
