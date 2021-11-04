using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Utilities;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ServiceBase.EventStore.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Implementations.EventProjections
{
    /// <summary>
    ///     Projection for all DomainEvents regarding <see cref="PreparedConfiguration" />
    /// </summary>
    public class ConfigurationEventProjection :
        EventProjectionBase,
        IDomainEventProjection<ConfigurationBuilt>
    {
        private readonly IConfigurationCompiler _compiler;
        private readonly ILogger<ConfigurationEventProjection> _logger;
        private readonly IDomainObjectStore _objectStore;
        private readonly IConfigurationParser _parser;
        private readonly IJsonTranslator _translator;

        /// <summary>
        ///     Create a new instance of <see cref="ConfigurationEventProjection" />
        /// </summary>
        /// <param name="compiler">compiler to compile new configs with</param>
        /// <param name="parser">parser used for <paramref name="compiler" /></param>
        /// <param name="translator">translator to generate json-views</param>
        /// <param name="objectStore">storage for generated configs</param>
        /// <param name="logger">logger to write diagnostic information</param>
        public ConfigurationEventProjection(
            IConfigurationCompiler compiler,
            IConfigurationParser parser,
            IJsonTranslator translator,
            IDomainObjectStore objectStore,
            ILogger<ConfigurationEventProjection> logger)
            : base(objectStore)
        {
            _compiler = compiler;
            _parser = parser;
            _translator = translator;
            _logger = logger;
            _objectStore = objectStore;
        }

        /// <inheritdoc />
        public async Task ProjectChanges(
            StreamedEventHeader eventHeader,
            IDomainEvent<ConfigurationBuilt> domainEvent)
        {
            _logger.LogDebug("version used during compilation: {ConfigurationVersion}", eventHeader.EventNumber);

            ConfigurationIdentifier configId = domainEvent.Payload.Identifier;

            // gather data to compile config with
            IResult<ConfigEnvironment> envResult = await _objectStore.Load<ConfigEnvironment, EnvironmentIdentifier>(configId.Environment);

            if (envResult.IsError)
            {
                _logger.LogWarning(
                    "unable to load environment to compile configuration {ConfigIdentifier}: {Code} {Message}",
                    configId,
                    envResult.Code,
                    envResult.Message);
                return;
            }

            IResult<ConfigStructure> structResult = await _objectStore.Load<ConfigStructure, StructureIdentifier>(configId.Structure);

            if (structResult.IsError)
            {
                _logger.LogWarning(
                    "unable to load structure to compile configuration {ConfigIdentifier}: {Code} {Message}",
                    configId,
                    envResult.Code,
                    envResult.Message);
                return;
            }

            ConfigEnvironment environment = envResult.CheckedData;
            ConfigStructure structure = structResult.CheckedData;

            try
            {
                // compile the actual config
                CompilationResult compilationResult = _compiler.Compile(
                    new EnvironmentCompilationInfo
                    {
                        Name = $"{configId.Environment.Category}/{configId.Environment.Name}",
                        Keys = environment.Keys.ToDictionary(kvp => kvp.Value.Key, kvp => kvp.Value.Value)
                    },
                    new StructureCompilationInfo
                    {
                        Name = $"{configId.Structure.Name}/{configId.Structure.Version}",
                        Keys = structure.Keys,
                        Variables = structure.Variables
                    },
                    _parser);

                // store result in DomainObject
                var config = new PreparedConfiguration(configId)
                {
                    ConfigurationVersion = (long)domainEvent.Timestamp
                                                            .Subtract(DateTime.UnixEpoch)
                                                            .TotalSeconds,
                    CurrentVersion = (long)eventHeader.EventNumber,
                    ValidFrom = domainEvent.Payload.ValidFrom,
                    ValidTo = domainEvent.Payload.ValidTo,
                    CreatedAt = domainEvent.Timestamp.ToUniversalTime(),
                    ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                    Keys = compilationResult.CompiledConfiguration.ToDictionary(),
                    Json = _translator.ToJson(compilationResult.CompiledConfiguration).ToString(),
                    UsedKeys = compilationResult.GetUsedKeys().ToList()
                };

                Stack<TraceResult> tracerStack = new(compilationResult.CompilationTrace);
                while (tracerStack.TryPop(out TraceResult? result))
                {
                    // most if not all traces will be returned as KeyTraceResult
                    if (result is not KeyTraceResult keyResult)
                    {
                        continue;
                    }

                    if (keyResult.Errors.Any())
                    {
                        if (keyResult.Key is null)
                        {
                            continue;
                        }

                        if (!config.Errors.TryGetValue(keyResult.Key, out List<string>? errorList))
                        {
                            errorList = new List<string>();
                            config.Errors[keyResult.Key] = errorList;
                        }

                        errorList.AddRange(keyResult.Errors);
                    }

                    if (keyResult.Warnings.Any())
                    {
                        if (keyResult.Key is null)
                        {
                            continue;
                        }

                        if (!config.Warnings.TryGetValue(keyResult.Key, out List<string>? warningList))
                        {
                            warningList = new List<string>();
                            config.Errors[keyResult.Key] = warningList;
                        }

                        warningList.AddRange(keyResult.Warnings);
                    }
                }

                await _objectStore.Store<PreparedConfiguration, ConfigurationIdentifier>(config);

                // update metadata
                IResult<IDictionary<string, string>>
                    metadataResult = await _objectStore.LoadMetadata<PreparedConfiguration, ConfigurationIdentifier>(config.Id);

                IDictionary<string, string> metadata;
                if (metadataResult.IsError)
                {
                    _logger.LogWarning("unable to update metadata for Configuration {ConfigurationIdentifier}, creating new one", config.Id);
                    metadata = new Dictionary<string, string>();
                }
                else
                {
                    metadata = metadataResult.CheckedData;
                }

                metadata["used_layers"] = JsonConvert.SerializeObject(environment.Layers);
                metadata["stale"] = JsonConvert.SerializeObject(false);

                await _objectStore.StoreMetadata<PreparedConfiguration, ConfigurationIdentifier>(config, metadata);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "failed to compile configuration, see exception for more details");
            }
        }
    }
}
