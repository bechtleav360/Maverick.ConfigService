using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Services
{
    public interface IStreamedStore
    {
        Task<IResult<StreamedEnvironmentList>> GetEnvironmentList();

        Task<IResult<StreamedEnvironment>> GetEnvironment(EnvironmentIdentifier identifier);

        Task<IResult<StreamedStructure>> GetStructure(StructureIdentifier identifier);

        Task<IResult<StreamedStructureList>> GetStructureList();
    }
}