using System;
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


namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Command("export", Description = "export data from the targeted ConfigService")]
    public class ExportCommand : SubCommand<Program>
    {
        /// <inheritdoc />
        public ExportCommand(IConsole console)
            : base(console)
        {
        }

        [Option("-e|--environment", Description = "Environment to export, given in \"{Category}/{Name}\" form")]
        public string[] Environments { get; set; }

        [Option("-o|--output", Description = "location to export data to")]
        public string OutputFile { get; set; }

        [Option("--format", Description = "interpret export and format it")]
        public ReformatKind Format { get; set; } = ReformatKind.None;

        public enum ReformatKind
        {
            None,
            Compress,
            Indent,
        }

        /// <inheritdoc />
        protected override bool CheckParameters()
        {
            // check Base-Parameters
            base.CheckParameters();

            if (Environments is null || !Environments.Any())
            {
                Output.WriteError($"no {nameof(Environments)} given -- see help for more information");
                return false;
            }

            // if environment doesn't contain '/' or contains multiple of them...
            var errEnvironments = Environments.Where(e => !e.Contains('/') ||
                                                          e.IndexOf('/') != e.LastIndexOf('/'))
                                              .ToArray();

            // ... complain about them
            if (errEnvironments.Any())
            {
                Output.WriteError($"given environments contain errors: {string.Join("; ", errEnvironments)}");
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            if (!CheckParameters())
                return 1;

            var exportDefinition = new ExportDefinition
            {
                Environments = Environments.Select(e =>
                                           {
                                               var split = e.Split('/');

                                               return new EnvironmentIdentifier(split[0], split[1]);
                                           })
                                           .ToArray()
            };

            var request = await RestRequest.Make(Output)
                                           .Post(new Uri(new Uri(ConfigServiceEndpoint), "export"),
                                                 new StringContent(JsonConvert.SerializeObject(exportDefinition, Formatting.None),
                                                                   Encoding.UTF8,
                                                                   "application/json"))
                                           .ReceiveString();

            var result = await request.Take<string>();

            if (Format != ReformatKind.None)
                result = Reformat(result, Format);

            if (request.HttpResponseMessage?.IsSuccessStatusCode != true)
            {
                Output.WriteError($"could not export environments '{string.Join("; ", Environments)}'");
                return 1;
            }

            // if no file is given, redirect to stdout
            if (string.IsNullOrWhiteSpace(OutputFile))
            {
                Output.Write(result);
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
                Output.WriteError($"could not write to file '{OutputFile}': {e}");
                return 1;
            }
        }

        private string Reformat(string raw, ReformatKind format)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject<ConfigExport>(raw);

                switch (format)
                {
                    case ReformatKind.Compress:
                        return JsonConvert.SerializeObject(obj, Formatting.None);

                    case ReformatKind.Indent:
                        return JsonConvert.SerializeObject(obj, Formatting.Indented);

                    default:
                        Output.WriteError($"unknown format '{format:G}'");
                        return raw;
                }
            }
            catch (JsonException e)
            {
                Output.WriteError($"can't re-interpret result: {e}");
                return raw;
            }
        }
    }
}