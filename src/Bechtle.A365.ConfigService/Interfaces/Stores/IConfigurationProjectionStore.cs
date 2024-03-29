﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Models.V1;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     read projected Configurations
    /// </summary>
    public interface IConfigurationProjectionStore : IDisposable, IAsyncDisposable
    {
        /// <summary>
        ///     build a new Configuration with the given Environment and Structure, valid in the given time-frame
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="validFrom"></param>
        /// <param name="validTo"></param>
        /// <returns></returns>
        Task<IResult> Build(ConfigurationIdentifier identifier, DateTime? validFrom, DateTime? validTo);

        /// <summary>
        ///     get a list of available projected Configurations
        /// </summary>
        /// <param name="when"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<IResult<Page<ConfigurationIdentifier>>> GetAvailable(DateTime when, QueryRange range);

        /// <summary>
        ///     get a list of available projected Configurations constrained to the specified Environment
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="when"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<IResult<Page<ConfigurationIdentifier>>> GetAvailableWithEnvironment(EnvironmentIdentifier environment, DateTime when, QueryRange range);

        /// <summary>
        ///     get a list of available projected Configurations constrained to the specified Structure
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="when"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<IResult<Page<ConfigurationIdentifier>>> GetAvailableWithStructure(StructureIdentifier structure, DateTime when, QueryRange range);

        /// <summary>
        ///     get the json of a Configuration
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="when"></param>
        /// <returns></returns>
        Task<IResult<JsonElement>> GetJson(ConfigurationIdentifier identifier, DateTime when);

        /// <summary>
        ///     get the keys of a Configuration
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="when"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<IResult<Page<KeyValuePair<string, string?>>>> GetKeys(ConfigurationIdentifier identifier, DateTime when, QueryRange range);

        /// <summary>
        ///     get configurations, that have stale configurations
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<IResult<Page<ConfigurationIdentifier>>> GetStale(QueryRange range);

        /// <summary>
        ///     get the used environment-keys for a configuration
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="when"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<IResult<Page<string>>> GetUsedConfigurationKeys(ConfigurationIdentifier identifier, DateTime when, QueryRange range);

        /// <summary>
        ///     get the version of a projected Configuration
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="when"></param>
        /// <returns></returns>
        Task<IResult<string>> GetVersion(ConfigurationIdentifier identifier, DateTime when);

        /// <summary>
        ///     check if the given configuration is currently stale or not
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult<bool>> IsStale(ConfigurationIdentifier identifier);

        /// <summary>
        ///     get metadata for all available configurations
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<IResult<Page<PreparedConfigurationMetadata>>> GetMetadata(QueryRange range);

        /// <summary>
        ///     get metadata for a given Configuration
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult<PreparedConfigurationMetadata>> GetMetadata(ConfigurationIdentifier identifier);
    }
}
