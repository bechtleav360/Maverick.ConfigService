using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ServiceBase.Commands;
using Bechtle.A365.Utilities.Rest;
using Bechtle.A365.Utilities.Rest.Extensions;
using Bechtle.A365.Utilities.Rest.Receivers;
using McMaster.Extensions.CommandLineUtils;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Command("configs", Description = "browse available configurations in the ConfigService")]
    public class BrowseConfigsCommand : SubCommand<BrowseCommand>
    {
        /// <inheritdoc />
        public BrowseConfigsCommand(IOutput output) : base(output)
        {
        }

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var request = await RestRequest.Make(Output)
                                           .Get(new Uri(new Uri(ConfigServiceEndpoint), "v1/configurations/available"))
                                           .ReceiveObject<Configuration[]>()
                                           .ReceiveString();

            if (request.HttpResponseMessage?.IsSuccessStatusCode == true)
            {
                var environments = await request.Take<Configuration[]>();

                if (environments is null)
                {
                    Output.WriteErrorLine("couldn't query available environments: " +
                                          $"{request.HttpResponseMessage?.StatusCode:D} " +
                                          $"({request.HttpResponseMessage?.StatusCode:G}); " +
                                          "can't convert response to target type");

                    var rawResponse = await request.Take<string>();
                    Output.WriteErrorLine($"couldn't deserialize response: {rawResponse}");

                    return 1;
                }

                Output.WriteTable(environments.OrderBy(e => e.Environment.Category)
                                              .ThenBy(e => e.Environment.Name)
                                              .ThenBy(e => e.Structure.Name)
                                              .ThenBy(e => e.Structure.Version),
                                  e => new Dictionary<string, object>
                                  {
                                      {"Category", e.Environment.Category},
                                      {"Environment", e.Environment.Name},
                                      {"Structure", e.Structure.Name},
                                      {"Version", e.Structure.Version}
                                  });

                return 0;
            }

            Output.WriteErrorLine("couldn't query available environments: " +
                                  $"{request.HttpResponseMessage?.StatusCode:D} " +
                                  $"({request.HttpResponseMessage?.StatusCode:G})");

            var response = await request.Take<string>();
            Output.WriteErrorLine(response ?? "{no response received}");

            return 1;
        }

        private class Configuration
        {
            public ConfigEnv Environment { get; set; } = null;

            public ConfigStruct Structure { get; set; } = null;
        }

        private class ConfigEnv
        {
            public string Category { get; set; } = string.Empty;

            public string Name { get; set; } = string.Empty;
        }

        public class ConfigStruct
        {
            public string Name { get; set; } = string.Empty;

            public int Version { get; set; } = 0;
        }
    }
}