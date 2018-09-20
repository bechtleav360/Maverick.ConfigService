using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto;

namespace Bechtle.A365.ConfigService.Projection
{
    /// <summary>
    /// </summary>
    public interface IConfigurationDatabase
    {
        /// <summary>
        /// </summary>
        Task Connect();

        /// <summary>
        /// </summary>
        /// <param name="environmentName"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        Task ModifyEnvironment(string environmentName, IEnumerable<ConfigKeyAction> actions);

        /// <summary>
        /// </summary>
        /// <param name="service"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        Task ModifySchema(string service, IEnumerable<ConfigKeyAction> actions);

        /// <summary>
        /// </summary>
        /// <param name="environmentName"></param>
        /// <returns></returns>
        Task<IDictionary<string, string>> GetEnvironment(string environmentName);

        /// <summary>
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        Task<IDictionary<string, string>> GetSchema(string schema);

        /// <summary>
        /// </summary>
        /// <param name="environmentName"></param>
        /// <param name="schema"></param>
        /// <param name="compiledVersion"></param>
        /// <returns></returns>
        Task SaveCompiledVersion(string environmentName, string schema, IDictionary<string, string> compiledVersion);
    }
}