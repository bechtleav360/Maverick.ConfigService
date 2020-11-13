using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <inheritdoc />
    public class DataImporter : IDataImporter
    {
        private readonly IDomainObjectStore _domainObjectStore;
        private readonly IEventStore _eventStore;
        private readonly ICommandValidator[] _validators;

        /// <inheritdoc cref="DataImporter" />
        public DataImporter(IEventStore eventStore,
                            IDomainObjectStore domainObjectStore,
                            IEnumerable<ICommandValidator> validators)
        {
            _eventStore = eventStore;
            _domainObjectStore = domainObjectStore;
            _validators = validators.ToArray();
        }

        /// <inheritdoc />
        public async Task<IResult> Import(ConfigExport export)
        {
            if (export is null)
                return Result.Error($"{nameof(export)} must not be null", ErrorCode.InvalidData);

            foreach (var layerExport in export.Layers)
            {
                var identifier = new LayerIdentifier(layerExport.Name);

                var layerResult = await _domainObjectStore.ReplayObject(new EnvironmentLayer(identifier), identifier.ToString());
                if (layerResult.IsError)
                    return layerResult;

                var layer = layerResult.Data;

                if (!layer.Created)
                    layer.Create();

                layer.ImportKeys(layerExport.Keys
                                            .Select(k => new EnvironmentLayerKey(k.Key, k.Value, k.Type, k.Description, 0))
                                            .ToList());

                var errors = layer.Validate(_validators);
                if (errors.Any())
                {
                    var errorMessages = string.Join("\r\n", errors.Values
                                                                  .SelectMany(_ => _)
                                                                  .Select(r => r.Message));

                    return Result.Error($"data-validation failed; {errorMessages}", ErrorCode.ValidationFailed);
                }

                var result = await layer.WriteRecordedEvents(_eventStore);
                if (result.IsError)
                    return result;
            }

            foreach (var envExport in export.Environments)
            {
                var identifier = new EnvironmentIdentifier(envExport.Category, envExport.Name);

                var envResult = await _domainObjectStore.ReplayObject(new ConfigEnvironment(identifier), identifier.ToString());
                if (envResult.IsError)
                    return envResult;

                var environment = envResult.Data;
                if (!environment.Created)
                    environment.Create();

                environment.AssignLayers(envExport.Layers);

                var errors = environment.Validate(_validators);
                if (errors.Any())
                {
                    var errorMessages = string.Join("\r\n", errors.Values
                                                                  .SelectMany(_ => _)
                                                                  .Select(r => r.Message));

                    return Result.Error($"data-validation failed; {errorMessages}", ErrorCode.ValidationFailed);
                }

                var result = await environment.WriteRecordedEvents(_eventStore);
                if (result.IsError)
                    return result;
            }

            return Result.Success();
        }
    }
}