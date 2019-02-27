using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.Utilities.Rest;
using Bechtle.A365.Utilities.Rest.Extensions;
using Bechtle.A365.Utilities.Rest.Receivers;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Cli
{
    [Command("export", Description = "export data from the targeted ConfigService")]
    public class ExportCommand : SubCommand<Program>
    {
        /// <inheritdoc />
        public ExportCommand(IConsole console)
            : base(console)
        {
        }

        [Required]
        [Option("-e|--environment", Description = "Environment to export, given in \"{Category}/{Name}\" form")]
        public string[] Environments { get; set; }

        [Option("-o|--output", Description = "location to export data to")]
        public string OutputFile { get; set; }

        /// <inheritdoc />
        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            if (string.IsNullOrWhiteSpace(Parent.ConfigServiceEndpoint))
            {
                LogError($"no {nameof(Parent.ConfigServiceEndpoint)} given -- see help for more information");
                return 1;
            }

            if (!Environments.Any())
            {
                LogError($"no {nameof(Environments)} given -- see help for more information");
                return 1;
            }

            // if environment doesn't contain '/' or contains multiple of them...
            var errEnvironments = Environments.Where(e => !e.Contains('/') || e.IndexOf('/') != e.LastIndexOf('/'))
                                              .ToArray();

            // ... complain about them
            if (errEnvironments.Any())
            {
                LogError($"given environments contain errors: {string.Join("; ", errEnvironments)}");
                return 1;
            }

            var exportDefinition = new ExportDefinition
            {
                Environments = Environments.Select(e =>
                                           {
                                               var split = e.Split('/');
                                               var category = split[0];
                                               var name = split[1];

                                               return new EnvironmentIdentifier
                                               {
                                                   Category = category,
                                                   Name = name
                                               };
                                           })
                                           .ToArray()
            };

            var request = await RestRequest.Make()
                                           .Post(new Uri(new Uri(Parent.ConfigServiceEndpoint), "export"),
                                                 new StringContent(JsonConvert.SerializeObject(exportDefinition, Formatting.None),
                                                                   Encoding.UTF8,
                                                                   "application/json"))
                                           .ReceiveString();

            if (request.HttpResponseMessage?.IsSuccessStatusCode != true)
            {
                LogError($"could not export environments '{string.Join("; ", Environments)}'");
                return 1;
            }

            // if no file is given, redirect to stdout
            if (string.IsNullOrWhiteSpace(OutputFile))
            {
                await Console.Out.WriteAsync(await request.Take<string>());
                return 0;
            }

            // if file is given write to it
            try
            {
                await File.WriteAllTextAsync(OutputFile, await request.Take<string>(), Encoding.UTF8);
                return 0;
            }
            catch (IOException e)
            {
                LogError($"could not write to file '{OutputFile}': {e}");
                return 1;
            }
        }
    }
}