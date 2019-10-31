using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    ///     Store to retrieve the latest available Version of a <see cref="StreamedObject"/>
    /// </summary>
    public interface IStreamedStore
    {
        /// <summary>
        ///     Get the latest Available <see cref="StreamedEnvironment"/> that matches the given <paramref name="identifier"/>
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult<StreamedEnvironment>> GetEnvironment(EnvironmentIdentifier identifier);

        /// <summary>
        ///     Get the latest Available <see cref="StreamedEnvironment"/> that matches the given <paramref name="identifier"/>
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="maxVersion"></param>
        /// <returns></returns>
        Task<IResult<StreamedEnvironment>> GetEnvironment(EnvironmentIdentifier identifier, long maxVersion);

        /// <summary>
        ///     Get an instance of <see cref="StreamedEnvironmentList"/> with the latest information about <see cref="StreamedEnvironment"/>
        /// </summary>
        /// <returns></returns>
        Task<IResult<StreamedEnvironmentList>> GetEnvironmentList();

        /// <summary>
        ///     Get an instance of <see cref="StreamedEnvironmentList"/> with the latest information about <see cref="StreamedEnvironment"/>
        /// </summary>
        /// <param name="maxVersion"></param>
        /// <returns></returns>
        Task<IResult<StreamedEnvironmentList>> GetEnvironmentList(long maxVersion);

        /// <summary>
        ///     Get the latest Available <see cref="StreamedStructure"/> that matches the given <paramref name="identifier"/>
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult<StreamedStructure>> GetStructure(StructureIdentifier identifier);

        /// <summary>
        ///     Get the latest Available <see cref="StreamedStructure"/> that matches the given <paramref name="identifier"/>
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="maxVersion"></param>
        /// <returns></returns>
        Task<IResult<StreamedStructure>> GetStructure(StructureIdentifier identifier, long maxVersion);

        /// <summary>
        ///     Get an instance of <see cref="StreamedStructureList"/> with the latest information about <see cref="StreamedStructure"/>
        /// </summary>
        /// <returns></returns>
        Task<IResult<StreamedStructureList>> GetStructureList();

        /// <summary>
        ///     Get an instance of <see cref="StreamedStructureList"/> with the latest information about <see cref="StreamedStructure"/>
        /// </summary>
        /// <param name="maxVersion"></param>
        /// <returns></returns>
        Task<IResult<StreamedStructureList>> GetStructureList(long maxVersion);

        /// <summary>
        ///     Get the latest Available <see cref="StreamedConfiguration"/> that matches the given <paramref name="identifier"/>
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult<StreamedConfiguration>> GetConfiguration(ConfigurationIdentifier identifier);

        /// <summary>
        ///     Get the latest Available <see cref="StreamedConfiguration"/> that matches the given <paramref name="identifier"/>
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="maxVersion"></param>
        /// <returns></returns>
        Task<IResult<StreamedConfiguration>> GetConfiguration(ConfigurationIdentifier identifier, long maxVersion);

        /// <summary>
        ///     Get an instance of <see cref="StreamedConfigurationList"/> with the latest information about <see cref="StreamedConfiguration"/>
        /// </summary>
        /// <returns></returns>
        Task<IResult<StreamedConfigurationList>> GetConfigurationList();

        /// <summary>
        ///     Get an instance of <see cref="StreamedConfigurationList"/> with the latest information about <see cref="StreamedConfiguration"/>
        /// </summary>
        /// <param name="maxVersion"></param>
        /// <returns></returns>
        Task<IResult<StreamedConfigurationList>> GetConfigurationList(long maxVersion);
    }
}