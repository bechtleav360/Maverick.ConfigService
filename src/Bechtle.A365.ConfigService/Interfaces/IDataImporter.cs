using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Objects;

namespace Bechtle.A365.ConfigService.Interfaces
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
}