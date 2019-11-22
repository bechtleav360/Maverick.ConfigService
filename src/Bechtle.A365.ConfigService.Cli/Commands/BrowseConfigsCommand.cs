using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public BrowseConfigsCommand(IConsole console) : base(console)
        {
        }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var request = await RestRequest.Make(Output)
                                           .Get(new Uri(new Uri(ConfigServiceEndpoint), "configurations/available"))
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
            else
            {
                Output.WriteErrorLine("couldn't query available environments: " +
                                      $"{request.HttpResponseMessage?.StatusCode:D} " +
                                      $"({request.HttpResponseMessage?.StatusCode:G})");

                return 1;
            }
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