﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    ///     read projected Configurations
    /// </summary>
    public interface IConfigurationProjectionStore
    {
        /// <summary>
        ///     get a list of available projected Configurations
        /// </summary>
        /// <param name="when"></param>
        /// <returns></returns>
        Task<Result<IList<ConfigurationIdentifier>>> GetAvailable(DateTime when);

        /// <summary>
        ///     get a list of available projected Configurations constrained to the specified Environment
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="when"></param>
        /// <returns></returns>
        Task<Result<IList<ConfigurationIdentifier>>> GetAvailableWithEnvironment(EnvironmentIdentifier environment, DateTime when);

        /// <summary>
        ///     get a list of available projected Configurations constrained to the specified Structure
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="when"></param>
        /// <returns></returns>
        Task<Result<IList<ConfigurationIdentifier>>> GetAvailableWithStructure(StructureIdentifier structure, DateTime when);

        /// <summary>
        ///     get the keys of a Configuration
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="when"></param>
        /// <returns></returns>
        Task<Result<IDictionary<string, string>>> GetKeys(ConfigurationIdentifier identifier, DateTime when);
    }
}