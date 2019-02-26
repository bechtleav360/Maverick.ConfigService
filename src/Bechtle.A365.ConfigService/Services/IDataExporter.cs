using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Dto;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    ///     export data from the current state of the DB
    /// </summary>
    public interface IDataExporter
    {
        /// <summary>
        ///     export data using the given definition
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        Task<IResult<ConfigExport>> Export(ExportDefinition definition);
    }

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
                    Keys = env.Data.ToArray()
                };

            _logger.LogWarning($"could not export data for environment '{id.Category}/{id.Name}'; {env.Code} {env.Message}");
            return new EnvironmentExport
            {
                Category = id.Category,
                Name = id.Name
            };
        }
    }

    /// <summary>
    ///     definition of what should be exported
    /// </summary>
    public class ExportDefinition
    {
        /// <summary>
        ///     list of environments that should be exported
        /// </summary>
        public EnvironmentIdentifier[] Environments { get; set; }
    }

    /// <summary>
    ///     collection of exported parts of the configuration
    /// </summary>
    public class ConfigExport
    {
        /// <inheritdoc cref="EnvironmentExport"/>
        public EnvironmentExport[] Environments { get; set; }
    }

    /// <summary>
    ///     a single Environment, exported for later import
    /// </summary>
    public class EnvironmentExport
    {
        /// <summary>
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// </summary>
        public DtoConfigKey[] Keys { get; set; }
    }
}