using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Cli.Commands.ConnectionChecks
{
    public class ConfigurationCheck : IConnectionCheck
    {
        /// <inheritdoc />
        public string Name => "Configuration Override";

        /// <inheritdoc />
        public Task<TestResult> Execute(IOutput output, TestParameters parameters, ApplicationSettings settings)
        {
            output.WriteLine("Showing effective Configuration in ConfigService");

            IConfigurationRoot config;

            if (parameters.Sources.Any())
            {
                output.WriteLine("Config-Source Hierarchy:", 1);
                foreach (var source in parameters.Sources)
                    output.WriteLine(source, 2);

                config = EvaluateSources(parameters);
            }
            else if (!string.IsNullOrWhiteSpace(parameters.UseDefaultConfigSources))
            {
                output.WriteLine("Config-Source Hierarchy (by Convention)", 1);
                config = EvaluateSourcesByConvention(parameters.UseDefaultConfigSources, parameters);
            }
            else
            {
                output.WriteLine("Config-Source Hierarchy (by Convention)", 1);
                config = EvaluateSourcesByConvention(null, parameters);
            }

            output.WriteLine($"Configuration loaded; '{config.AsEnumerable().Count()}' entries from '{config.Providers.Count()}' providers", 1);
            output.WriteLine("Providers:", 1);

            foreach (var provider in config.Providers)
            {
                if (provider is JsonConfigurationProvider json)
                    output.WriteLine($"[{nameof(JsonConfigurationProvider)}; " +
                                     $"{json.Source.Path}; " +
                                     $"{(json.Source.Optional ? "Optional" : "Required")}; " +
                                     $"{(json.Source.ReloadOnChange ? "Reload" : "Once")}]",
                                     2);
                else
                    output.WriteLine($"[{provider.GetType().Name}]", 2);
            }

            output.WriteLine(string.Empty, 1);

            var orderedConfigKeys = config.AsEnumerable()
                                          .OrderBy(e => e.Key)
                                          .ToList();

            var longestKey = orderedConfigKeys.Max(e => e.Key.Length);

            output.WriteLine("Effective Configuration:", 1);
            foreach (var (key, value) in orderedConfigKeys)
                output.WriteLine($"{key.PadRight(longestKey)} => {value}", 2);

            output.WriteLine(string.Empty, 1);

            output.WriteLine($"Applying Effective Configuration to {nameof(ConfigServiceConfiguration)}", 1);

            var csConfig = new ConfigServiceConfiguration();
            try
            {
                config.Bind(csConfig);
            }
            catch (Exception e)
            {
                output.WriteLine($"Effective Configuration could not be applied to {nameof(ConfigServiceConfiguration)}", 1);
                output.WriteLine($"Error: {e.GetType().Name}; {e.Message}", 1);
            }

            var configJson = JsonConvert.SerializeObject(csConfig, Formatting.Indented);
            output.WriteLine($"Effective Configuration:{Environment.NewLine}{configJson}", 1);

            output.WriteLine(string.Empty, 1);

            output.WriteLine("Setting Effective Configuration for later Checks...", 1);

            settings.EffectiveConfiguration = config;

            output.WriteLine(string.Empty, 1);

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
        ///         file:appsettings.json;req
        ///         file:appsettings.json;required
        ///         file:../../appsettings.json
        ///         file:../../appsettings.json;req
        ///         file:../../appsettings.json;required
        ///         file:C:\A365\Service\appsettings.json
        ///         file:C:\A365\Service\appsettings.json;req
        ///         file:C:\A365\Service\appsettings.json;required
        ///         file:"C:\A365\Service\appsettings.json"
        ///         file:"C:\A365\Service\appsettings.json";req
        ///         file:"C:\A365\Service\appsettings.json";required
        ///         file:"C:\A365\Service with spaces\appsettings.json"
        ///         file:"C:\A365\Service with spaces\appsettings.json";req
        ///         file:"C:\A365\Service with spaces\appsettings.json";required
        ///     </example>
        /// </summary>
        private static readonly Regex FileSourceMatcher = new Regex(@"^file\:(?<file>([\w\d\.\:\\\/]+)|(""[\s\w\d\.\:\\\/]+""))(?<required>\;req(uired)?)?$",
                                                                    RegexOptions.Compiled);

        /// <summary>
        ///     match against the pattern cli-args
        ///     <example>
        ///         cli-args
        ///     </example>
        /// </summary>
        private static readonly Regex CommandLineArgumentMatcher = new Regex(@"^cli-args$",
                                                                             RegexOptions.Compiled);

        private IConfigurationRoot EvaluateSourcesByConvention(string root, TestParameters parameters)
            => (string.IsNullOrWhiteSpace(root)
                    ? new ConfigurationBuilder()
                      .AddEnvironmentVariables()
                      .AddCommandLine(parameters.PassThruArguments)
                    : new ConfigurationBuilder()
                      .AddJsonFile(Path.Combine(root, "appsettings.json"), true)
                      .AddJsonFile(Path.Combine(root, "appsettings.development.json"), true)
                      .AddEnvironmentVariables()
                      .AddCommandLine(parameters.PassThruArguments))
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
                    // file is optional if group <required> is not successful
                    builder.AddJsonFile(fileMatch.Groups["file"].Value,
                                        !fileMatch.Groups["required"].Success);
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