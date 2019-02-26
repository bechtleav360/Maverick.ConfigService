using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    ///     import previously exported data
    /// </summary>
    public interface IDataImporter
    {
        /// <summary>
        ///     import previously exported data
        /// </summary>
        /// <param name="export"></param>
        /// <returns></returns>
        Task<IResult> Import(ConfigExport export);
    }

    /// <inheritdoc />
    public class DataImporter : IDataImporter
    {
        private readonly IEventStore _store;

        /// <inheritdoc />
        public DataImporter(IEventStore store)
        {
            _store = store;
        }

        /// <inheritdoc />
        public async Task<IResult> Import(ConfigExport export)
        {
            if (export is null)
                return Result.Error($"{nameof(export)} must not be null", ErrorCode.InvalidData);

            foreach (var environment in export.Environments)
            {
                await new ConfigEnvironment().IdentifiedBy(new EnvironmentIdentifier(environment.Category, environment.Name))
                                             .ImportKeys(environment.Keys)
                                             .Save(_store);
            }

            return Result.Success();
        }
    }
}