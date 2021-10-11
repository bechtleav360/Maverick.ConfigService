using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Models.V1;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <inheritdoc />
    public class DataExporter : IDataExporter
    {
        private readonly ILogger<DataExporter> _logger;
        private readonly IProjectionStore _store;

        /// <inheritdoc cref="DataExporter" />
        public DataExporter(
            ILogger<DataExporter> logger,
            IProjectionStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IResult<ConfigExport>> Export(ExportDefinition definition)
        {
            var result = new ConfigExport();

            if (definition.Environments.Any())
            {
                result.Environments = await ExportInternal(definition.Environments);
            }

            if (definition.Layers.Any())
            {
                result.Layers = await ExportInternal(definition.Layers);
            }

            return Result.Success(result);
        }

        private async Task<LayerExport[]> ExportInternal(IEnumerable<LayerIdentifier> layers)
        {
            Task<LayerExport>[] tasks = layers.Select(ExportInternal).ToArray();

            await Task.WhenAll(tasks);

            return tasks.Select(t => t.Result)
                        .ToArray();
        }

        private async Task<LayerExport> ExportInternal(LayerIdentifier layer)
        {
            IResult<Page<DtoConfigKey>> dataResult = await _store.Layers.GetKeyObjects(
                                                         new KeyQueryParameters<LayerIdentifier>
                                                         {
                                                             Identifier = layer,
                                                             Range = QueryRange.All
                                                         });

            if (dataResult.IsError)
            {
                _logger.LogWarning($"could not export key-data for layer {layer}");
                return new LayerExport
                {
                    Name = layer.Name,
                    Keys = Array.Empty<EnvironmentKeyExport>()
                };
            }

            return new LayerExport
            {
                Name = layer.Name,
                Keys = dataResult.CheckedData
                                 .Items
                                 .Select(
                                     k => new EnvironmentKeyExport
                                     {
                                         Key = k.Key,
                                         Value = k.Value,
                                         Description = k.Description,
                                         Type = k.Type
                                     })
                                 .ToArray()
            };
        }

        private async Task<EnvironmentExport[]> ExportInternal(IEnumerable<EnvironmentIdentifier> environments)
        {
            Task<EnvironmentExport>[] tasks = environments.Select(ExportInternal).ToArray();

            await Task.WhenAll(tasks);

            return tasks.Select(t => t.Result)
                        .ToArray();
        }

        private async Task<EnvironmentExport> ExportInternal(EnvironmentIdentifier id)
        {
            IResult<Page<LayerIdentifier>> env = await _store.Environments.GetAssignedLayers(id);

            if (env.IsError)
            {
                _logger.LogWarning($"could not export assigned layers for environment '{id.Category}/{id.Name}'; {env.Code} {env.Message}");
                return new EnvironmentExport
                {
                    Category = id.Category,
                    Name = id.Name
                };
            }

            return new EnvironmentExport
            {
                Category = id.Category,
                Name = id.Name,
                Layers = env.CheckedData.Items.ToArray()
            };
        }
    }
}
