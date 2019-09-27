using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Metrics;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Events;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Bechtle.A365.ConfigService.Projection.Metrics;
using Bechtle.A365.ConfigService.Projection.Services;
using Bechtle.A365.Core.EventBus.Events.Messages;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class ConfigurationBuiltHandler : IDomainEventHandler<ConfigurationBuilt>
    {
        private readonly IConfigurationCompiler _compiler;
        private readonly IConfigurationDatabase _database;
        private readonly IEventBusService _eventBus;
        private readonly ILogger<ConfigurationBuiltHandler> _logger;
        private readonly IMetrics _metrics;
        private readonly IConfigurationParser _parser;
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public ConfigurationBuiltHandler(IConfigurationDatabase database,
                                         IConfigurationCompiler compiler,
                                         IConfigurationParser parser,
                                         IJsonTranslator translator,
                                         IEventBusService eventBus,
                                         IMetrics metrics,
                                         ILogger<ConfigurationBuiltHandler> logger)
        {
            _database = database;
            _compiler = compiler;
            _parser = parser;
            _translator = translator;
            _eventBus = eventBus;
            _metrics = metrics;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(ConfigurationBuilt domainEvent)
        {
            if (domainEvent is null)
                throw new ArgumentNullException(nameof(domainEvent), $"{nameof(domainEvent)} must not be null");

            var envId = domainEvent.Identifier.Environment;
            var structId = domainEvent.Identifier.Structure;

            _logger.LogInformation($"building Configuration for {envId} {structId}");

            var structureResult = await _database.GetStructure(structId);
            if (structureResult.IsError)
                throw new Exception(structureResult.Message);

            var environmentResult = await _database.GetEnvironmentWithInheritance(envId);
            if (environmentResult.IsError)
                throw new Exception(environmentResult.Message);

            var structureSnapshot = structureResult.Data;
            var environmentSnapshot = environmentResult.Data;

            var environmentInfo = new EnvironmentCompilationInfo
            {
                Name = $"{envId.Category}/{envId.Name}",
                Keys = environmentSnapshot.Data
            };

            var structureInfo = new StructureCompilationInfo
            {
                Name = $"{structId.Name}/{structId.Version}",
                Keys = structureSnapshot.Data,
                Variables = structureSnapshot.Variables
            };

            var compiled = _compiler.Compile(environmentInfo,
                                             structureInfo,
                                             _parser);

            IncrementCompilerWarningCounter(compiled);

            var json = _translator.ToJson(compiled.CompiledConfiguration)
                                  .ToString();

            await _database.SaveConfiguration(environmentSnapshot,
                                              structureSnapshot,
                                              compiled.CompiledConfiguration,
                                              json,
                                              compiled.GetUsedKeys(),
                                              domainEvent.ValidFrom,
                                              domainEvent.ValidTo);

            await PublishConfigurationChangedEvent(domainEvent);
        }

        private void IncrementCompilerWarningCounter(CompilationResult compiled)
        {
            var traceList = new List<TraceResult>(compiled.CompiledConfiguration.Count);
            var stack = new Stack<TraceResult>();
            // prepare stack with initial data
            foreach (var item in compiled.CompilationTrace)
                stack.Push(item);

            while (stack.TryPop(out var item))
            {
                traceList.Add(item);
                foreach (var child in item.Children)
                    stack.Push(child);
            }

            _metrics.Measure.Counter.Increment(KnownMetrics.CompilerMessages,
                                               traceList.OfType<KeyTraceResult>()
                                                        .Sum(traceResult => traceResult.Warnings.Length),
                                               "Warnings");

            _metrics.Measure.Counter.Increment(KnownMetrics.CompilerMessages,
                                               traceList.OfType<KeyTraceResult>()
                                                        .Sum(traceResult => traceResult.Errors.Length),
                                               "Errors");
        }

        private async Task PublishConfigurationChangedEvent(ConfigurationBuilt domainEvent)
        {
            /*
             * null => null       =    publish
             * null => earlier    = no publish
             * null => later      =    publish
             * earlier => null    =    publish
             * earlier => earlier = no publish
             * earlier => later   =    publish
             * later => null      = no publish
             * later => earlier   = no publish - invalid range
             * later => later     = no publish
             */
            var from = domainEvent.ValidFrom;
            var to = domainEvent.ValidTo;
            var now = DateTime.UtcNow;

            if (from is null && to is null ||
                from is null && to >= now ||
                from <= now && to is null ||
                from <= now && to >= now)
                await _eventBus.Publish(new EventMessage
                {
                    Event = new OnConfigurationPublished
                    {
                        EnvironmentCategory = domainEvent.Identifier.Environment.Category,
                        EnvironmentName = domainEvent.Identifier.Environment.Name,
                        StructureName = domainEvent.Identifier.Structure.Name,
                        StructureVersion = domainEvent.Identifier.Structure.Version
                    }
                });
        }
    }
}