﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Services
{
    /// <inheritdoc />
    public class DataExporter : IDataExporter
    {
        private readonly IProjectionStore _store;
        private readonly ILogger<DataExporter> _logger;

        /// <inheritdoc />
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

        private async Task<EnvironmentExport[]> ExportInternal(IEnumerable<EnvironmentIdentifier> environments)
        {
            var tasks = environments.Select(ExportInternal).ToArray();

            await Task.WhenAll(tasks);

            return tasks.Select(t => t.Result)
                        .ToArray();
        }

        private async Task<EnvironmentExport> ExportInternal(EnvironmentIdentifier id)
        {
            var env = await _store.Environments.GetKeyObjects(id, QueryRange.All);

            if (!env.IsError)
                return new EnvironmentExport
                {
                    Category = id.Category,
                    Name = id.Name,
                    Keys = env.Data
                              .Select(k=>new EnvironmentKeyExport
                              {
                                  Key = k.Key,
                                  Description = k.Description,
                                  Type = k.Type,
                                  Value = k.Value
                              })
                              .ToArray()
                };

            _logger.LogWarning($"could not export data for environment '{id.Category}/{id.Name}'; {env.Code} {env.Message}");
            return new EnvironmentExport
            {
                Category = id.Category,
                Name = id.Name
            };
        }
    }
}