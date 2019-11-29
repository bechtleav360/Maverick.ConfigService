using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Parsing;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <inheritdoc />
    public sealed class ConfigurationProjectionStore : IConfigurationProjectionStore
    {
        private readonly IConfigurationCompiler _compiler;
        private readonly IDomainObjectStore _domainObjectStore;
        private readonly IEventStore _eventStore;
        private readonly ILogger<ConfigurationProjectionStore> _logger;
        private readonly IConfigurationParser _parser;
        private readonly IJsonTranslator _translator;
        private readonly IList<ICommandValidator> _validators;

        /// <inheritdoc />
        public ConfigurationProjectionStore(ILogger<ConfigurationProjectionStore> logger,
                                            IDomainObjectStore domainObjectStore,
                                            IConfigurationCompiler compiler,
                                            IConfigurationParser parser,
                                            IJsonTranslator translator,
                                            IEventStore eventStore,
                                            IEnumerable<ICommandValidator> validators)
        {
            _logger = logger;
            _domainObjectStore = domainObjectStore;
            _compiler = compiler;
            _parser = parser;
            _translator = translator;
            _eventStore = eventStore;
            _validators = validators.ToList();
        }

        /// <inheritdoc />
        public async Task<IResult> Build(ConfigurationIdentifier identifier, DateTime? validFrom, DateTime? validTo)
        {
            _logger.LogDebug($"building new '{nameof(PreparedConfiguration)}' using '{identifier}', from={validFrom}, to={validTo}");

            var configResult = await _domainObjectStore.ReplayObject(new PreparedConfiguration(identifier), identifier.ToString());
            if (configResult.IsError)
                return configResult;

            var configuration = configResult.Data;

            var buildResult = configuration.Build(validFrom, validTo);
            if (buildResult.IsError)
                return buildResult;

            // assumeLatestVersion=true, because otherwise it would use CurrentVersion of an non-replayed DomainObject
            // which defaults to -1 causing errors while getting target-env and target-struct
            //
            // because this configuration *will* be added to the stream, we can assume that we will get the *most up to date objects as of now*
            await configuration.Compile(_domainObjectStore, _compiler, _parser, _translator, _logger, true);

            _logger.LogDebug("validating resulting events");
            var errors = configuration.Validate(_validators);
            if (errors.Any())
                return Result.Error("failed to validate generated DomainEvents",
                                    ErrorCode.ValidationFailed,
                                    errors.Values
                                          .SelectMany(_ => _)
                                          .ToList());

            return await configuration.WriteRecordedEvents(_eventStore);
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
            try
            {
                _logger.LogDebug($"collecting available configurations at '{when:O}', range={range}");

                var list = await _domainObjectStore.ReplayObject<PreparedConfigurationList>();
                if (list.IsError)
                    return Result.Success<IList<ConfigurationIdentifier>>(new List<ConfigurationIdentifier>());

                var utcWhen = when.ToUniversalTime();
                _logger.LogDebug($"using utc-time={utcWhen:O}");

                var identifiers =
                    list.Data
                        .GetIdentifiers()
                        .Where(pair => (pair.Value.ValidFrom ?? DateTime.MinValue) <= utcWhen
                                       && (pair.Value.ValidTo ?? DateTime.MaxValue) >= utcWhen)
                        .OrderBy(pair => pair.Key.Environment.Category)
                        .ThenBy(pair => pair.Key.Environment.Name)
                        .ThenBy(pair => pair.Key.Structure.Name)
                        .ThenByDescending(s => s.Key.Structure.Version)
                        .Skip(range.Offset)
                        .Take(range.Length)
                        .Select(pair => pair.Key)
                        .ToList();

                _logger.LogDebug($"collected '{identifiers.Count}' identifiers");

                return Result.Success<IList<ConfigurationIdentifier>>(identifiers);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configurations");
                return Result.Error<IList<ConfigurationIdentifier>>("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ConfigurationIdentifier>>> GetAvailableWithEnvironment(EnvironmentIdentifier environment,
                                                                                               DateTime when,
                                                                                               QueryRange range)
        {
            try
            {
                _logger.LogDebug($"collecting available configurations at '{when:O}', range={range}");

                var list = await _domainObjectStore.ReplayObject<PreparedConfigurationList>();
                if (list.IsError)
                    return Result.Success<IList<ConfigurationIdentifier>>(new List<ConfigurationIdentifier>());

                var utcWhen = when.ToUniversalTime();
                _logger.LogDebug($"using utc-time={utcWhen:O}");

                var identifiers =
                    list.Data
                        .GetIdentifiers()
                        .Where(pair => (pair.Value.ValidFrom ?? DateTime.MinValue) <= utcWhen
                                       && (pair.Value.ValidTo ?? DateTime.MaxValue) >= utcWhen)
                        .Where(pair => pair.Key.Environment.Category == environment.Category
                                       && pair.Key.Environment.Name == environment.Name)
                        .OrderBy(pair => pair.Key.Environment.Category)
                        .ThenBy(pair => pair.Key.Environment.Name)
                        .ThenBy(pair => pair.Key.Structure.Name)
                        .ThenByDescending(s => s.Key.Structure.Version)
                        .Skip(range.Offset)
                        .Take(range.Length)
                        .Select(pair => pair.Key)
                        .ToList();

                _logger.LogDebug($"collected '{identifiers.Count}' identifiers");

                return Result.Success<IList<ConfigurationIdentifier>>(identifiers);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configurations");
                return Result.Error<IList<ConfigurationIdentifier>>("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ConfigurationIdentifier>>> GetAvailableWithStructure(StructureIdentifier structure,
                                                                                             DateTime when,
                                                                                             QueryRange range)
        {
            try
            {
                _logger.LogDebug($"collecting available configurations at '{when:O}', range={range}");

                var list = await _domainObjectStore.ReplayObject<PreparedConfigurationList>();
                if (list.IsError)
                    return Result.Success<IList<ConfigurationIdentifier>>(new List<ConfigurationIdentifier>());

                var utcWhen = when.ToUniversalTime();
                _logger.LogDebug($"using utc-time={utcWhen:O}");

                var identifiers =
                    list.Data
                        .GetIdentifiers()
                        .Where(pair => (pair.Value.ValidFrom ?? DateTime.MinValue) <= utcWhen
                                       && (pair.Value.ValidTo ?? DateTime.MaxValue) >= utcWhen)
                        .Where(pair => pair.Key.Structure.Name == structure.Name
                                       && pair.Key.Structure.Version == structure.Version)
                        .OrderBy(pair => pair.Key.Environment.Category)
                        .ThenBy(pair => pair.Key.Environment.Name)
                        .ThenBy(pair => pair.Key.Structure.Name)
                        .ThenByDescending(s => s.Key.Structure.Version)
                        .Skip(range.Offset)
                        .Take(range.Length)
                        .Select(pair => pair.Key)
                        .ToList();

                _logger.LogDebug($"collected '{identifiers.Count}' identifiers");

                return Result.Success<IList<ConfigurationIdentifier>>(identifiers);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configurations");
                return Result.Error<IList<ConfigurationIdentifier>>("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<JsonElement>> GetJson(ConfigurationIdentifier identifier, DateTime when)
        {
            var formattedParams = "(" +
                                  $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Category)}: {identifier.Environment.Category}; " +
                                  $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Name)}: {identifier.Environment.Name}; " +
                                  $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Name)}: {identifier.Structure.Name}; " +
                                  $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Version)}: {identifier.Structure.Version}" +
                                  ")";

            try
            {
                _logger.LogDebug($"getting json of '{identifier}' at {when:O}");

                var configuration = await _domainObjectStore.ReplayObject(new PreparedConfiguration(identifier), identifier.ToString());
                if (configuration.IsError || !configuration.Data.Created)
                    return Result.Error<JsonElement>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                _logger.LogDebug($"compiling '{identifier}'");
                await configuration.Data.Compile(_domainObjectStore, _compiler, _parser, _translator, _logger);

                if (configuration.Data.Json is null)
                    return Result.Error<JsonElement>($"no json-data found for configuration with id: {formattedParams}", ErrorCode.NotFound);

                _logger.LogDebug($"parsing Config-Keys to json");
                var jsonElement = JsonDocument.Parse(configuration.Data.Json);

                return Result.Success(jsonElement.RootElement);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"failed to retrieve projected configuration keys for id: {formattedParams}");
                return Result.Error<JsonElement>($"failed to retrieve projected configuration keys for id: {formattedParams}",
                                                 ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IDictionary<string, string>>> GetKeys(ConfigurationIdentifier identifier,
                                                                        DateTime when,
                                                                        QueryRange range)
        {
            var formattedParams = "(" +
                                  $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Category)}: {identifier.Environment.Category}; " +
                                  $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Name)}: {identifier.Environment.Name}; " +
                                  $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Name)}: {identifier.Structure.Name}; " +
                                  $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Version)}: {identifier.Structure.Version}" +
                                  ")";

            try
            {
                _logger.LogDebug($"retrieving keys of '{identifier}' at {when:O}, range={range}");

                var configuration = await _domainObjectStore.ReplayObject(new PreparedConfiguration(identifier), identifier.ToString());
                if (configuration.IsError || !configuration.Data.Created)
                    return Result.Error<IDictionary<string, string>>(
                        $"no configuration found with id: {formattedParams}",
                        ErrorCode.NotFound);

                _logger.LogDebug($"compiling '{identifier}'");
                await configuration.Data.Compile(_domainObjectStore, _compiler, _parser, _translator, _logger);

                if (configuration.Data.Keys is null || !configuration.Data.Keys.Any())
                    return Result.Error<IDictionary<string, string>>(
                        $"no data found for configuration with id: {formattedParams}",
                        ErrorCode.NotFound);

                var result = configuration.Data
                                          .Keys
                                          .OrderBy(k => k.Key)
                                          .Skip(range.Offset)
                                          .Take(range.Length)
                                          .ToImmutableSortedDictionary(k => k.Key, k => k.Value, StringComparer.OrdinalIgnoreCase);

                _logger.LogDebug($"collected '{result.Count}' keys from '{identifier}'");

                return Result.Success<IDictionary<string, string>>(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"failed to retrieve projected configuration keys for id: {formattedParams}");
                return Result.Error<IDictionary<string, string>>($"failed to retrieve projected configuration keys for id: {formattedParams}",
                                                                 ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ConfigurationIdentifier>>> GetStale(QueryRange range)
        {
            try
            {
                _logger.LogDebug($"retrieving stale configurations, range={range}");

                var list = await _domainObjectStore.ReplayObject<PreparedConfigurationList>();
                if (list.IsError)
                    return Result.Success<IList<ConfigurationIdentifier>>(new List<ConfigurationIdentifier>());

                var stale = list.Data.GetStale();

                _logger.LogDebug($"got '{stale.Count}' stale configurations, filtering / ordering");

                var identifiers = stale.OrderBy(id => id.Environment.Category)
                                       .ThenBy(id => id.Environment.Name)
                                       .ThenBy(id => id.Structure.Name)
                                       .ThenByDescending(id => id.Structure.Version)
                                       .Skip(range.Offset)
                                       .Take(range.Length)
                                       .ToList();

                _logger.LogDebug($"collected '{identifiers.Count}' identifiers");

                return Result.Success<IList<ConfigurationIdentifier>>(identifiers);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configurations");
                return Result.Error<IList<ConfigurationIdentifier>>("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IEnumerable<string>>> GetUsedConfigurationKeys(ConfigurationIdentifier identifier,
                                                                                 DateTime when,
                                                                                 QueryRange range)
        {
            var formattedParams = "(" +
                                  $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Category)}: {identifier.Environment.Category}; " +
                                  $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Name)}: {identifier.Environment.Name}; " +
                                  $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Name)}: {identifier.Structure.Name}; " +
                                  $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Version)}: {identifier.Structure.Version}" +
                                  ")";

            try
            {
                _logger.LogDebug($"retrieving used Env-Keys for configuration '{identifier}'");

                var configuration = await _domainObjectStore.ReplayObject(new PreparedConfiguration(identifier), identifier.ToString());
                if (configuration.IsError || !configuration.Data.Created)
                    return Result.Error<IEnumerable<string>>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                _logger.LogDebug($"compiling '{identifier}'");
                await configuration.Data.Compile(_domainObjectStore, _compiler, _parser, _translator, _logger);

                if (configuration.Data.UsedKeys is null || !configuration.Data.UsedKeys.Any())
                    return Result.Error<IEnumerable<string>>($"no used-keys for configuration with id: {formattedParams}", ErrorCode.NotFound);

                var result = configuration.Data
                                          .UsedKeys
                                          .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                                          .Skip(range.Offset)
                                          .Take(range.Length)
                                          .ToArray();

                _logger.LogDebug($"collected '{result.Length}' keys");

                return Result.Success<IEnumerable<string>>(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"failed to retrieve used environment keys for id: {formattedParams}");
                return Result.Error<IEnumerable<string>>($"failed to retrieve used environment keys for id: {formattedParams}: {e}",
                                                         ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<string>> GetVersion(ConfigurationIdentifier identifier, DateTime when)
        {
            var formattedParams = "(" +
                                  $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Category)}: {identifier.Environment.Category}; " +
                                  $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Name)}: {identifier.Environment.Name}; " +
                                  $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Name)}: {identifier.Structure.Name}; " +
                                  $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Version)}: {identifier.Structure.Version}" +
                                  ")";

            try
            {
                _logger.LogDebug($"retrieving Config-Version of '{identifier}' at {when:O}");

                var configuration = await _domainObjectStore.ReplayObject(new PreparedConfiguration(identifier), identifier.ToString());
                if (configuration.IsError || !configuration.Data.Created)
                    return Result.Error<string>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                var result = configuration.Data.ConfigurationVersion.ToString();

                _logger.LogDebug($"Config-Version of '{identifier}' = '{result}'");

                return Result.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"failed to retrieve used environment keys for id: {formattedParams}");
                return Result.Error<string>($"failed to retrieve used environment keys for id: {formattedParams}: {e}",
                                            ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<bool>> IsStale(ConfigurationIdentifier identifier)
        {
            try
            {
                var list = await _domainObjectStore.ReplayObject<PreparedConfigurationList>();
                if (list.IsError)
                {
                    _logger.LogInformation($"could not retrieve ConfigurationList to determine staleness of '{identifier}', defaulting to true");
                    return Result.Success(true);
                }

                // if it's on the list of known stale Configs return Stale / True
                if (list.Data
                        .GetStale()
                        .Any(id => id.Equals(identifier)))
                    return Result.Success(true);

                // if it's not, but still known to us, it's not Stale / False
                if (list.Data.GetIdentifiers().Keys.Any(id => id.Equals(identifier)))
                    return Result.Success(false);

                // otherwise it defaults to Stale / True
                return Result.Success(true);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configurations");
                return Result.Error<bool>("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }
    }
}