using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ServiceBase.Commands;
using Bechtle.A365.Utilities.Rest;
using Bechtle.A365.Utilities.Rest.Extensions;
using Bechtle.A365.Utilities.Rest.Receivers;
using McMaster.Extensions.CommandLineUtils;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Command("environments", Description = "browse available environments in the ConfigService")]
    public class BrowseEnvironmentsCommand : SubCommand<BrowseCommand>
    {
        /// <inheritdoc />
        public BrowseEnvironmentsCommand(IOutput output) : base(output)
        {
        }

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var request = await RestRequest.Make(Output)
                                           .Get(new Uri(new Uri(ConfigServiceEndpoint), "v1/environments/available"))
                                           .ReceiveObject<ConfigEnv[]>()
                                           .ReceiveString();

            if (request.HttpResponseMessage?.IsSuccessStatusCode == true)
            {
                var environments = await request.Take<ConfigEnv[]>();

                if (environments is null)
                {
                    Output.WriteErrorLine("couldn't query available environments: " +
                                          $"{request.HttpResponseMessage?.StatusCode:D} " +
                                          $"({request.HttpResponseMessage?.StatusCode:G}); " +
                                          "can't convert response to target type");

                    return 1;
                }

                Output.WriteTable(environments, e => new Dictionary<string, object> {{"Category", e.Category}, {"Name", e.Name}});

                return 0;
            }

            Output.WriteErrorLine("couldn't query available environments: " +
                                  $"{request.HttpResponseMessage?.StatusCode:D} " +
                                  $"({request.HttpResponseMessage?.StatusCode:G})");

            return 1;
        }

        private class ConfigEnv
        {
            public string Category { get; set; } = string.Empty;

            public string Name { get; set; } = string.Empty;
        }
    }
}