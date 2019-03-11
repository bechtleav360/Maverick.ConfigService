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
        /// <inheritdoc />
        public TestCommand(IConsole console)
            : base(console)
        {
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

            var output = new FormattedOutput(Logger);
            var parameters = new TestParameters
            {
                ConfigServiceEndpoint = ConfigServiceEndpoint,
                Sources = ConfigSources,
                UseDefaultConfigSources = UseDefaultConfigSources,
                PassThruArguments = app.RemainingArguments.ToArray()
            };

            output.Line("Checking Connection to ConfigService");
            output.Separator();
            output.Line($"using Config-Service = '{ConfigServiceEndpoint}'");

            var results = new Dictionary<string, TestResult>();
            foreach (var test in GetConnectionChecks())
            {
                output.Line(0);
                try
                {
                    results.Add(test.Name, await test.Execute(output, parameters));
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

            output.Separator();
            output.Line("Results:");

            var longestKeyLength = results.Max(r => r.Key.Length);

            foreach (var (name, testResult) in results)
                output.Line($"{name.PadRight(longestKeyLength, ' ')} => {(testResult.Result ? "Pass" : "Fail")}", 1);

            return results.All(r => r.Value.Result) ? 0 : 1;
        }

        private IEnumerable<IConnectionCheck> GetConnectionChecks() => new IConnectionCheck[]
        {
            new ConfigurationCheck(),
            new SwaggerAvailabilityCheck(),
            new EventStoreConnectionCheck(),
            new DatabaseCheck(), 
        };
    }
}