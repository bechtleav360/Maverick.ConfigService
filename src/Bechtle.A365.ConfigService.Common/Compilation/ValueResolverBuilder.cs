using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    /// <summary>
    ///     builder for <see cref="IValueResolver" /> implementations
    /// </summary>
    public class ValueResolverBuilder
    {
        private readonly Dictionary<ConfigValueProviderType, IConfigValueProvider> _valueProviders;
        private EnvironmentCompilationInfo _environment;
        private ILogger<IValueResolver> _logger;
        private StructureCompilationInfo _structure;

        // restrict external access
        private ValueResolverBuilder()
        {
            _environment = null;
            _structure = null;
            _logger = null;
            _valueProviders = new Dictionary<ConfigValueProviderType, IConfigValueProvider>();
        }

        /// <summary>
        ///     create a new instance of <see cref="ValueResolverBuilder" />
        /// </summary>
        /// <returns></returns>
        public static ValueResolverBuilder CreateNew() => new ValueResolverBuilder();

        /// <summary>
        ///     build a new instance of <see cref="DefaultValueResolver" /> with the previously given information
        /// </summary>
        /// <returns></returns>
        public IValueResolver BuildDefault()
        {
            if (_environment is null) throw new ArgumentException("environment cannot be null");
            if (_structure is null) throw new ArgumentException("structure cannot be null");

            return new DefaultValueResolver(_environment, _structure, _valueProviders, _logger);
        }

        /// <summary>
        ///     clear the list of providers
        /// </summary>
        /// <returns></returns>
        public ValueResolverBuilder ClearProviders()
        {
            _valueProviders.Clear();
            return this;
        }

        /// <summary>
        ///     set the current instance to use the given <see cref="EnvironmentCompilationInfo" />.
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        public ValueResolverBuilder UseEnvironment(EnvironmentCompilationInfo environment)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            return this;
        }

        /// <summary>
        ///     use the default <see cref="IConfigValueProvider" /> for providing Environment-Keys (<see cref="EnvironmentValueProvider" />).
        ///     register a valid <see cref="EnvironmentCompilationInfo" /> before using this method
        /// </summary>
        /// <returns></returns>
        public ValueResolverBuilder UseEnvironmentKeyProvider()
            => UseValueProvider(ConfigValueProviderType.Environment, new EnvironmentValueProvider(_environment.Keys));

        /// <summary>
        ///     use the given logger for newly built <see cref="IValueResolver" /> instances
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public ValueResolverBuilder UseLogger(ILogger<IValueResolver> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            return this;
        }

        /// <summary>
        ///     set the current instance to use the given <see cref="StructureCompilationInfo" />
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public ValueResolverBuilder UseStructure(StructureCompilationInfo structure)
        {
            _structure = structure ?? throw new ArgumentNullException(nameof(structure));
            return this;
        }

        /// <summary>
        ///     use the default <see cref="IConfigValueProvider" /> for providing Structure-Variables (<see cref="StructureVariableValueProvider" />).
        ///     register a valid <see cref="StructureCompilationInfo" /> before using this method
        /// </summary>
        /// <returns></returns>
        public ValueResolverBuilder UseStructureVariableProvider()
            => UseValueProvider(ConfigValueProviderType.StructVariables, new StructureVariableValueProvider(_structure.Variables));

        /// <summary>
        ///     register a specific <see cref="IConfigValueProvider" /> for the given slot
        /// </summary>
        /// <param name="type"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public ValueResolverBuilder UseValueProvider(ConfigValueProviderType type, IConfigValueProvider provider)
        {
            _valueProviders[type] = provider ?? throw new ArgumentNullException(nameof(provider));
            return this;
        }
    }
}