using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <inheritdoc />
    public class DataExporter : IDataExporter
    {
        private readonly ILogger<DataExporter> _logger;
        private readonly IProjectionStore _store;

        /// <inheritdoc cref="DataExporter"/>
        public DataExporter(ILogger<DataExporter> logger,
                            IProjectionStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IResult<ConfigExport>> Export(ExportDefinition definition)
        {
            var result = new ConfigExport();

            if (definition is null)
                return Result.Error<ConfigExport>($"{nameof(definition)} must not be null", ErrorCode.InvalidData);

            if (definition.Environments.Any())
                result.Environments = await ExportInternal(definition.Environments);

            return Result.Success(result);
        }

        private async Task<EnvironmentExport[]> ExportInternal(IEnumerable<EnvironmentExportDefinition> environments)
        {
            var tasks = environments.Select(ExportInternal).ToArray();

            await Task.WhenAll(tasks);

            return tasks.Select(t => t.Result)
                        .ToArray();
        }

        private async Task<EnvironmentExport> ExportInternal(EnvironmentExportDefinition envExport)
        {
            var id = new EnvironmentIdentifier(envExport.Category, envExport.Name);

            var env = await _store.Environments.GetKeyObjects(new EnvironmentKeyQueryParameters
            {
                Environment = id,
                Range = QueryRange.All
            });

            if (env.IsError)
            {
                _logger.LogWarning($"could not export data for environment '{id.Category}/{id.Name}'; {env.Code} {env.Message}");
                return new EnvironmentExport
                {
                    Category = id.Category,
                    Name = id.Name
                };
            }

            Func<DtoConfigKey, bool> selector;

            // if the list is null or empty we will export everything
            // if some data is available, we will filter the current Env for those keys
            if (envExport.Keys is null || !envExport.Keys.Any())
                selector = _ => true;
            else
                selector = entry => envExport.Keys.Any(export => export.Equals(entry.Key, StringComparison.OrdinalIgnoreCase));

            return new EnvironmentExport
            {
                Category = id.Category,
                Name = id.Name,
                Keys = env.Data
                          .Where(selector)
                          .Select(k => new EnvironmentKeyExport
                          {
                              Key = k.Key,
                              Description = k.Description,
                              Type = k.Type,
                              Value = k.Value
                          })
                          .ToArray()
            };
        }
    }
}