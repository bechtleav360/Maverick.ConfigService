﻿using Bechtle.A365.ConfigService.Parsing;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    /// <summary>
    ///     Component that compiles a Config-Structure with data from an Environment
    /// </summary>
    public interface IConfigurationCompiler
    {
        /// <summary>
        ///     compile a big data-set from two separate components
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="structure"></param>
        /// <param name="parser"></param>
        /// <returns></returns>
        CompilationResult Compile(EnvironmentCompilationInfo environment,
                                  StructureCompilationInfo structure,
                                  IConfigurationParser parser);
    }
}