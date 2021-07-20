using System;
using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Tracers;
using Bechtle.A365.ConfigService.Parsing;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    /// <summary>
    ///     Base-Implementation of <see cref="IConfigurationCompiler"/>
    /// </summary>
    public class ConfigurationCompiler : IConfigurationCompiler
    {
        private readonly ISecretConfigValueProvider _secretProvider;
        private readonly ILogger<ConfigurationCompiler> _logger;
        private readonly ILogger<IValueResolver> _resolverLogger;

        /// <summary>
        ///     Creates a new instance of <see cref="ConfigurationCompiler"/>
        /// </summary>
        /// <param name="secretProvider">instance of <see cref="ISecretConfigValueProvider"/> that provides values for "$secret/"-references</param>
        /// <param name="logger">logger-instance to write diagnostic-messages to</param>
        /// <param name="resolverLogger">logger-instance passed to a private instance of <see cref="IValueResolver"/></param>
        public ConfigurationCompiler(ISecretConfigValueProvider secretProvider,
                                     ILogger<ConfigurationCompiler> logger,
                                     ILogger<IValueResolver> resolverLogger)
        {
            _secretProvider = secretProvider;
            _logger = logger;
            _resolverLogger = resolverLogger;
        }

        /// <inheritdoc />
        public CompilationResult Compile(EnvironmentCompilationInfo environment,
                                         StructureCompilationInfo structure,
                                         IConfigurationParser parser)
        {
            _logger.LogInformation($"compiling environment '{environment.Name}' ({environment.Keys.Count} entries) " +
                                   $"and structure '{structure.Name}' ({structure.Keys.Count} entries)");

            ICompilationTracer compilationTracer = new CompilationTracer();
            var resolver = ValueResolverBuilder.CreateNew()
                                               .UseEnvironment(environment)
                                               .UseStructure(structure)
                                               .UseLogger(_resolverLogger)
                                               .UseEnvironmentKeyProvider()
                                               .UseStructureVariableProvider()
                                               .UseSecretProvider(_secretProvider)
                                               .BuildDefault();

            var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in structure.Keys)
            {
                var result = resolver.Resolve(key, value, compilationTracer.AddKey(key, value), parser).RunSync();
                if (result.IsError)
                    _logger.LogWarning(result.Message);

                foreach (var (rk, rv) in result.Data)
                    configuration[rk] = rv;
            }

            return new CompilationResult(configuration, compilationTracer.GetResults());
        }
    }
}