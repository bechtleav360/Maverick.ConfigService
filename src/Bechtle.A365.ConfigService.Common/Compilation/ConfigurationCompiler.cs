using System;
using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Tracers;
using Bechtle.A365.ConfigService.Parsing;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    public class ConfigurationCompiler : IConfigurationCompiler
    {
        private readonly ILogger<ConfigurationCompiler> _logger;
        private readonly ILogger<IValueResolver> _resolverLogger;

        public ConfigurationCompiler(ILogger<ConfigurationCompiler> logger, ILogger<IValueResolver> resolverLogger)
        {
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