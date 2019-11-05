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
using Bechtle.A365.ConfigService.Parsing;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Services.Stores
{
    /// <inheritdoc />
    public class ConfigurationProjectionStore : IConfigurationProjectionStore
    {
        private readonly IStreamedStore _streamedStore;
        private readonly IConfigurationCompiler _compiler;
        private readonly IConfigurationParser _parser;
        private readonly IJsonTranslator _translator;
        private readonly IEventStore _eventStore;
        private readonly IList<ICommandValidator> _validators;
        private readonly ILogger<ConfigurationProjectionStore> _logger;

        /// <inheritdoc />
        public ConfigurationProjectionStore(ILogger<ConfigurationProjectionStore> logger,
                                            IStreamedStore streamedStore,
                                            IConfigurationCompiler compiler,
                                            IConfigurationParser parser,
                                            IJsonTranslator translator,
                                            IEventStore eventStore,
                                            IEnumerable<ICommandValidator> validators)
        {
            _logger = logger;
            _streamedStore = streamedStore;
            _compiler = compiler;
            _parser = parser;
            _translator = translator;
            _eventStore = eventStore;
            _validators = validators.ToList();
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ConfigurationIdentifier>>> GetAvailable(DateTime when, QueryRange range)
        {
            try
            {
                var list = await _streamedStore.GetConfigurationList();
                if (list.IsError)
                    return Result.Success<IList<ConfigurationIdentifier>>(new List<ConfigurationIdentifier>());

                var utcWhen = when.ToUniversalTime();
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
                var list = await _streamedStore.GetConfigurationList();
                if (list.IsError)
                    return Result.Success<IList<ConfigurationIdentifier>>(new List<ConfigurationIdentifier>());

                var utcWhen = when.ToUniversalTime();
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
                var list = await _streamedStore.GetConfigurationList();
                if (list.IsError)
                    return Result.Success<IList<ConfigurationIdentifier>>(new List<ConfigurationIdentifier>());

                var utcWhen = when.ToUniversalTime();
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
                var configuration = await _streamedStore.GetConfiguration(identifier);
                if (configuration.IsError)
                    return Result.Error<JsonElement>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                await configuration.Data.Compile(_streamedStore, _compiler, _parser, _translator, _logger);

                if (configuration.Data.Json is null)
                    return Result.Error<JsonElement>($"no json-data found for configuration with id: {formattedParams}", ErrorCode.NotFound);

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
                var configuration = await _streamedStore.GetConfiguration(identifier);
                if (configuration.IsError)
                    return Result.Error<IDictionary<string, string>>(
                        $"no configuration found with id: {formattedParams}",
                        ErrorCode.NotFound);

                await configuration.Data.Compile(_streamedStore, _compiler, _parser, _translator, _logger);

                if (configuration.Data.Keys is null || !configuration.Data.Keys.Any())
                    return Result.Error<IDictionary<string, string>>(
                        $"no data found for configuration with id: {formattedParams}",
                        ErrorCode.NotFound);

                return Result.Success<IDictionary<string, string>>(
                    configuration.Data
                                 .Keys
                                 .OrderBy(k => k.Key)
                                 .Skip(range.Offset)
                                 .Take(range.Length)
                                 .ToImmutableSortedDictionary(k => k.Key, k => k.Value, StringComparer.OrdinalIgnoreCase));
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
                var list = await _streamedStore.GetConfigurationList();
                if (list.IsError)
                    return Result.Success<IList<ConfigurationIdentifier>>(new List<ConfigurationIdentifier>());

                var identifiers =
                    list.Data
                        .GetStale()
                        .OrderBy(id => id.Environment.Category)
                        .ThenBy(id => id.Environment.Name)
                        .ThenBy(id => id.Structure.Name)
                        .ThenByDescending(id => id.Structure.Version)
                        .Skip(range.Offset)
                        .Take(range.Length)
                        .ToList();

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
                var configuration = await _streamedStore.GetConfiguration(identifier);
                if (configuration.IsError)
                    return Result.Error<IEnumerable<string>>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                await configuration.Data.Compile(_streamedStore, _compiler, _parser, _translator, _logger);

                if (configuration.Data.UsedKeys is null || !configuration.Data.UsedKeys.Any())
                    return Result.Error<IEnumerable<string>>($"no used-keys for configuration with id: {formattedParams}", ErrorCode.NotFound);

                return Result.Success<IEnumerable<string>>(configuration.Data
                                                                        .UsedKeys
                                                                        .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                                                                        .Skip(range.Offset)
                                                                        .Take(range.Length)
                                                                        .ToArray());
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
                var configuration = await _streamedStore.GetConfiguration(identifier);
                if (configuration.IsError)
                    return Result.Error<string>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                return Result.Success(configuration.Data.ConfigurationVersion.ToString());
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"failed to retrieve used environment keys for id: {formattedParams}");
                return Result.Error<string>($"failed to retrieve used environment keys for id: {formattedParams}: {e}",
                                            ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult> Build(ConfigurationIdentifier identifier, DateTime? validFrom, DateTime? validTo)
        {
            var configResult = await _streamedStore.GetConfiguration(identifier);
            if (configResult.IsError)
                return configResult;

            var configuration = configResult.Data;

            var buildResult = configuration.Build(validFrom, validTo);
            if (buildResult.IsError)
                return buildResult;

            var errors = configuration.Validate(_validators);
            if (errors.Any())
                return Result.Error("failed to validate generated DomainEvents",
                                    ErrorCode.ValidationFailed,
                                    errors.Values
                                          .SelectMany(_ => _)
                                          .ToList());

            return await configuration.WriteRecordedEvents(_eventStore);
        }
    }
}