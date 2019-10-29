using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Services.Stores;

namespace Bechtle.A365.ConfigService.Services
{
    /// <inheritdoc />
    public class DataImporter : IDataImporter
    {
        private readonly IEventStore _eventStore;
        private readonly IStreamedStore _streamedStore;
        private readonly ICommandValidator[] _validators;

        /// <inheritdoc />
        public DataImporter(IEventStore eventStore,
                            IStreamedStore streamedStore,
                            IEnumerable<ICommandValidator> validators)
        {
            _eventStore = eventStore;
            _streamedStore = streamedStore;
            _validators = validators.ToArray();
        }

        /// <inheritdoc />
        public async Task<IResult> Import(ConfigExport export)
        {
            if (export is null)
                return Result.Error($"{nameof(export)} must not be null", ErrorCode.InvalidData);

            foreach (var envExport in export.Environments)
            {
                var identifier = new EnvironmentIdentifier(envExport.Category, envExport.Name);

                var envResult = await _streamedStore.GetEnvironment(identifier);
                if (envResult.IsError)
                    return envResult;

                var environment = envResult.Data;

                environment.ImportKeys(envExport.Keys
                                                .Select(k => new StreamedEnvironmentKey
                                                {
                                                    Key = k.Key,
                                                    Description = k.Description,
                                                    Type = k.Type,
                                                    Value = k.Value
                                                })
                                                .ToList());

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