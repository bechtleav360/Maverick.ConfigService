using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Services
{
    public interface IStreamedStore
    {
        Task<IResult<StreamedEnvironment>> GetEnvironment(EnvironmentIdentifier identifier);
        
        Task<IResult<StreamedEnvironment>> GetEnvironment(EnvironmentIdentifier identifier, long maxVersion);

        Task<IResult<StreamedEnvironmentList>> GetEnvironmentList();

        Task<IResult<StreamedEnvironmentList>> GetEnvironmentList(long maxVersion);

        Task<IResult<StreamedStructure>> GetStructure(StructureIdentifier identifier);

        Task<IResult<StreamedStructure>> GetStructure(StructureIdentifier identifier, long maxVersion);

        Task<IResult<StreamedStructureList>> GetStructureList();

        Task<IResult<StreamedStructureList>> GetStructureList(long maxVersion);

        Task<IResult<StreamedConfiguration>> GetConfiguration(ConfigurationIdentifier identifier);

        Task<IResult<StreamedConfiguration>> GetConfiguration(ConfigurationIdentifier identifier, long maxVersion);

        Task<IResult<StreamedConfigurationList>> GetConfigurationList();

        Task<IResult<StreamedConfigurationList>> GetConfigurationList(long maxVersion);
    }
}