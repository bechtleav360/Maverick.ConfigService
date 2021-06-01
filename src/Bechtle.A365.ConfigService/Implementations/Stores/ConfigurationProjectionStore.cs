﻿using System;
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
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <inheritdoc />
    public sealed class ConfigurationProjectionStore : IConfigurationProjectionStore
    {
        private readonly IDomainObjectManager _domainObjectManager;
        private readonly IDomainObjectStore _domainObjectStore;
        private readonly ILogger<ConfigurationProjectionStore> _logger;

        /// <inheritdoc cref="ConfigurationProjectionStore" />
        public ConfigurationProjectionStore(
            ILogger<ConfigurationProjectionStore> logger,
            IDomainObjectStore domainObjectStore,
            IDomainObjectManager domainObjectManager)
        {
            _logger = logger;
            _domainObjectStore = domainObjectStore;
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
            _domainObjectStore?.Dispose();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_domainObjectStore != null)
                await _domainObjectStore.DisposeAsync();
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ConfigurationIdentifier>>> GetAvailable(DateTime when, QueryRange range)
        {
            _logger.LogDebug(
                "collecting available configurations at '{When:O}', range={Range}",
                when,
                range);

            return await _domainObjectManager.GetConfigurations(range, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ConfigurationIdentifier>>> GetAvailableWithEnvironment(
            EnvironmentIdentifier environment,
            DateTime when,
            QueryRange range)
        {
            _logger.LogDebug("collecting available configurations with {Identifier} at '{When}', range={Range}", environment, when, range);

            return await _domainObjectManager.GetConfigurations(environment, range, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ConfigurationIdentifier>>> GetAvailableWithStructure(
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

                return Result.Success(JsonDocument.Parse(configuration.Data.Json).RootElement);
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
        public async Task<IResult<IDictionary<string, string>>> GetKeys(
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
                    return Result.Error<IDictionary<string, string>>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                return Result.Success(configuration.Data.Keys);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configuration keys for id: {Identifier}", identifier);
                return Result.Error<IDictionary<string, string>>(
                    $"failed to retrieve projected configuration keys for id: {formattedParams}",
                    ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ConfigurationIdentifier>>> GetStale(QueryRange range)
        {
            try
            {
                // @TODO: add GetStaleConfigurations in IDomainObjectManager
                _logger.LogDebug($"retrieving stale configurations, range={range}");
                return Result.Success<IList<ConfigurationIdentifier>>(new List<ConfigurationIdentifier>());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configurations");
                return Result.Error<IList<ConfigurationIdentifier>>("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IEnumerable<string>>> GetUsedConfigurationKeys(
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
                    return Result.Error<IEnumerable<string>>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                var result = configuration.Data
                                          .UsedKeys
                                          .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                                          .Skip(range.Offset)
                                          .Take(range.Length)
                                          .ToArray();

                _logger.LogDebug("collected '{UsedKeys}' keys", result.Length);

                return Result.Success<IEnumerable<string>>(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve used environment keys for id: {Identifier}", identifier);
                return Result.Error<IEnumerable<string>>(
                    $"failed to retrieve used environment keys for id: {formattedParams}: {e}",
                    ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<string>> GetVersion(ConfigurationIdentifier identifier, DateTime when)
        {
            var formattedParams = "("
                                  + $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Category)}: {identifier.Environment.Category}; "
                                  + $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Name)}: {identifier.Environment.Name}; "
                                  + $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Name)}: {identifier.Structure.Name}; "
                                  + $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Version)}: {identifier.Structure.Version}"
                                  + ")";

            try
            {
                _logger.LogDebug("retrieving Config-Version of '{Identifier}' at {When}", identifier, when);

                IResult<PreparedConfiguration> configuration = await _domainObjectManager.GetConfiguration(identifier, CancellationToken.None);
                if (configuration.IsError)
                    return Result.Error<string>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                var result = configuration.Data.ConfigurationVersion.ToString();

                _logger.LogDebug("Config-Version of {Identifier} = {Version}", identifier, result);

                return Result.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"failed to retrieve used environment keys for id: {formattedParams}");
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
                // @TODO: implement IDomainObjectManager.IsStale
                return Result.Success(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configurations");
                return Result.Error<bool>("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }
    }
}
