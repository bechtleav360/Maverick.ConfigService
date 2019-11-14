using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Cli.Commands.ConnectionChecks;
using McMaster.Extensions.CommandLineUtils;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Command("test",
        Description = "Test connection *to* ConfigService and from ConfigService to Database",
        ThrowOnUnexpectedArgument = false,
        AllowArgumentSeparator = true)]
    public class TestCommand : SubCommand<Program>
    {
        private readonly ApplicationSettings _settings;

        /// <inheritdoc />
        public TestCommand(IConsole console, ApplicationSettings settings)
            : base(console)
        {
            _settings = settings;
        }

        [Option("-c|--config", Description = "Config-Source to use, multiple values possible")]
        public string[] ConfigSources { get; set; } = new string[0];

        [Option("--config-by-convention", Description = "Add Config-Sources according to the .NetCore Conventions.\r\n" +
                                                        "Value represents ConfigService Root-Directory.\r\n" +
                                                        "appsettings.json / appsettings.json / Environment-Variables / Command-Line Arguments")]
        public string UseDefaultConfigSources { get; set; } = null;

        /// <inheritdoc />
        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            if (!CheckParameters())
                return 1;

            var parameters = new TestParameters
            {
                ConfigServiceEndpoint = ConfigServiceEndpoint,
                Sources = ConfigSources,
                UseDefaultConfigSources = UseDefaultConfigSources,
                PassThruArguments = app.RemainingArguments.ToArray()
            };

            Output.WriteLine("Checking Connection to ConfigService");
            Output.WriteSeparator();
            Output.WriteLine($"using Config-Service = '{ConfigServiceEndpoint}'");

            var results = new Dictionary<string, TestResult>();
            foreach (var test in GetConnectionChecks())
            {
                Output.WriteLine(string.Empty);
                try
                {
                    results.Add(test.Name, await test.Execute(Output, parameters, _settings));
                }
                catch (Exception e)
                {
                    results.Add(test.Name, new TestResult
                    {
                        Result = false,
                        Message = $"exception '{e.GetType().Name}' thrown: {e.Message}"
                    });
                }
            }

            Output.WriteSeparator();
            Output.WriteLine("Results:");

            var longestKeyLength = results.Max(r => r.Key.Length);

            foreach (var (name, testResult) in results)
                Output.WriteLine($"{name.PadRight(longestKeyLength, ' ')} => {(testResult.Result ? "Pass" : "Fail")}", 1);

            return results.All(r => r.Value.Result) ? 0 : 1;
        }

        private IEnumerable<IConnectionCheck> GetConnectionChecks() => new IConnectionCheck[]
        {
            new ConfigurationCheck(),
            new SwaggerAvailabilityCheck(),
            new EventStoreConnectionCheck(),
        };
    }
}