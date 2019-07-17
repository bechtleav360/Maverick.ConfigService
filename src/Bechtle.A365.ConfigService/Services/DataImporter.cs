using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Dto;

namespace Bechtle.A365.ConfigService.Services
{
    /// <inheritdoc />
    public class DataImporter : IDataImporter
    {
        private readonly IEventStore _store;
        private readonly ICommandValidator[] _validators;

        /// <inheritdoc />
        public DataImporter(IEventStore store,
                            IEnumerable<ICommandValidator> validators)
        {
            _store = store;
            _validators = validators.ToArray();
        }

        /// <inheritdoc />
        public async Task<IResult> Import(ConfigExport export)
        {
            if (export is null)
                return Result.Error($"{nameof(export)} must not be null", ErrorCode.InvalidData);

            foreach (var environment in export.Environments)
            {
                var domainObj = new ConfigEnvironment().IdentifiedBy(new EnvironmentIdentifier(environment.Category, environment.Name))
                                                       .ImportKeys(environment.Keys
                                                                              .Select(k => new DtoConfigKey
                                                                              {
                                                                                  Key = k.Key,
                                                                                  Description = k.Description,
                                                                                  Type = k.Type,
                                                                                  Value = k.Value
                                                                              }));


                var errors = domainObj.Validate(_validators);
                if (errors.Any())
                {
                    var errorMessages = string.Join("\r\n", errors.Values
                                                                  .SelectMany(_ => _)
                                                                  .Select(r => r.Message));

                    return Result.Error($"data-validation failed; {errorMessages}", ErrorCode.ValidationFailed);
                }

                await domainObj.Save(_store);
            }

            return Result.Success();
        }
    }
}