using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Objects;

namespace Bechtle.A365.ConfigService.Interfaces
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
}