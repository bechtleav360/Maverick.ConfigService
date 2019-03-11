using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Configuration;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Cli.Commands.ConnectionChecks
{
    public class ConfigurationCheck : IConnectionCheck
    {
        public static IConfiguration EffectiveConfiguration { get; private set; }

        /// <inheritdoc />
        public string Name => "Configuration Override";

        /// <inheritdoc />
        public Task<TestResult> Execute(FormattedOutput output, TestParameters parameters)
        {
            output.Line("Showing effective Configuration in ConfigService");

            IConfigurationRoot config;

            if (string.IsNullOrWhiteSpace(parameters.UseDefaultConfigSources))
            {
                output.Line("Config-Source Hierarchy:", 1);
                foreach (var source in parameters.Sources)
                    output.Line(source, 2);

                config = EvaluateSources(parameters);
            }
            else
            {
                output.Line("Config-Source Hierarchy (by Convention)", 1);
                config = EvaluateSourcesByConvention(parameters.UseDefaultConfigSources, parameters);
            }

            output.Line($"Configuration loaded; '{config.AsEnumerable().Count()}' entries from '{config.Providers.Count()}' providers", 1);
            output.Line("Providers:", 1);

            foreach (var provider in config.Providers)
                output.Line($"{provider.GetType().Name}", 2);

            output.Line(1);

            output.Line("Effective Configuration:", 1);
            foreach (var (key, value) in config.AsEnumerable().OrderBy(e => e.Key))
                output.Line($"{key} => {value}", 2);

            output.Line(1);

            output.Line($"Applying Effective Configuration to {nameof(ConfigServiceConfiguration)}", 1);
            
            var csConfig = new ConfigServiceConfiguration();
            try
            {
                config.Bind(csConfig);
            }
            catch (Exception e)
            {
                output.Line($"Effective Configuration could not be applied to {nameof(ConfigServiceConfiguration)}", 1);
                output.Line($"Error: {e.GetType().Name}; {e.Message}", 1);
            }

            var configJson = JsonConvert.SerializeObject(csConfig, Formatting.Indented);
            output.Line($"Effective Configuration:\r\n{configJson}", 1);

            output.Line(1);

            output.Line("Setting Effective Configuration for later Checks...", 1);
            
            EffectiveConfiguration = config;
            
            output.Line(1);

            return Task.FromResult(new TestResult
            {
                Result = true,
                Message = ""
            });
        }

        /// <summary>
        ///     match against the pattern env[ironment][:{\w\d}]
        ///     <example>
        ///         env
        ///         env:ASPNETCORE_
        ///         environment
        ///         environment:ASPNETCORE_
        ///     </example>
        /// </summary>
        private static readonly Regex EnvironmentSourceMatcher = new Regex(@"^env(ironment)?(\:(?<env>[\w\d]+))?$",
                                                                           RegexOptions.Compiled);

        /// <summary>
        ///     match against the pattern file:{filename.ext}[;opt]
        ///     <example>
        ///         file:appsettings.json
        ///         file:appsettings.json;opt
        ///         file:../../appsettings.json
        ///         file:../../appsettings.json;opt
        ///         file:C:\A365\Service\appsettings.json
        ///         file:C:\A365\Service\appsettings.json;opt
        ///         file:"C:\A365\Service\appsettings.json"
        ///         file:"C:\A365\Service\appsettings.json";opt
        ///         file:"C:\A365\Service with spaces\appsettings.json"
        ///         file:"C:\A365\Service with spaces\appsettings.json";opt
        ///     </example>
        /// </summary>
        private static readonly Regex FileSourceMatcher = new Regex(@"^file\:(?<file>([\w\d\.\:\\\/]+)|(""[\s\w\d\.\:\\\/] +""))(?<opt>\;opt)?$",
                                                                    RegexOptions.Compiled);

        /// <summary>
        ///     match against the pattern file:{filename.ext}[;opt]
        ///     <example>
        ///         file:filename.ext
        ///         file:filename.ext;opt
        ///     </example>
        /// </summary>
        private static readonly Regex CommandLineArgumentMatcher = new Regex(@"^cli-args$",
                                                                             RegexOptions.Compiled);

        private IConfigurationRoot EvaluateSourcesByConvention(string root, TestParameters parameters)
            => new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(root, "appsettings.json"), true)
               .AddJsonFile(Path.Combine(root, "appsettings.development.json"), true)
               .AddEnvironmentVariables()
               .AddCommandLine(parameters.PassThruArguments)
               .Build();

        private IConfigurationRoot EvaluateSources(TestParameters parameters)
        {
            var builder = new ConfigurationBuilder();

            foreach (var source in parameters.Sources)
            {
                var environmentMatch = EnvironmentSourceMatcher.Match(source);
                if (environmentMatch.Success)
                {
                    builder.AddEnvironmentVariables(environmentMatch.Groups["env"].Value);
                    continue;
                }

                var fileMatch = FileSourceMatcher.Match(source);
                if (fileMatch.Success)
                {
                    builder.AddJsonFile(fileMatch.Groups["file"].Value,
                                        fileMatch.Groups["opt"].Success);
                    continue;
                }

                var argMatch = CommandLineArgumentMatcher.Match(source);
                if (argMatch.Success)
                    builder.AddCommandLine(parameters.PassThruArguments);
            }

            return builder.Build();
        }
    }
}