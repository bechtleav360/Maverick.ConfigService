using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ServiceBase.Commands;
using Bechtle.A365.Utilities.Rest;
using Bechtle.A365.Utilities.Rest.Extensions;
using Bechtle.A365.Utilities.Rest.Receivers;
using McMaster.Extensions.CommandLineUtils;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Command("export", Description = "export data from the targeted ConfigService")]
    public class ExportCommand : SubCommand<CliBase>
    {
        public enum ReformatKind
        {
            None,
            Compress,
            Indent
        }

        /// <inheritdoc />
        public ExportCommand(IOutput output) : base(output)
        {
        }

        [Option("-e|--environment", Description = "Environment to export, given in \"{Category}/{Name}\" form")]
        public string[] Environments { get; set; }

        [Option("--format", Description = "interpret export and format it")]
        public ReformatKind Format { get; set; } = ReformatKind.None;

        [Option("-l|--layer", Description = "Layer to export")]
        public string[] Layers { get; set; }

        [Option("-o|--output", Description = "location to export data to")]
        public string OutputFile { get; set; }

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
        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            if (!CheckParameters())
                return 1;

            var selectedEnvironments = new List<EnvironmentIdentifier>(Environments.Length);
            var selectedLayers = Layers.Select(l => new LayerIdentifier(l)).ToArray();

            foreach (var environment in Environments)
                try
                {
                    selectedEnvironments.Add(CreateExportDefinition(environment));
                }
                catch (Exception e)
                {
                    Output.WriteError(e.Message);
                    return 1;
                }

            var exportDefinition = new ExportDefinition {Environments = selectedEnvironments.ToArray(), Layers = selectedLayers};

            var request = await RestRequest.Make(Output)
                                           .Post(new Uri(new Uri(ConfigServiceEndpoint), "v1/export"),
                                                 new StringContent(
                                                     JsonSerializer.Serialize(exportDefinition,
                                                                              new JsonSerializerOptions {WriteIndented = false}),
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

        /// <summary>
        ///     environment-id in {Category}/{Name} format
        /// </summary>
        /// <param name="environmentIdentifier"></param>
        /// <returns></returns>
        private EnvironmentIdentifier CreateExportDefinition(string environmentIdentifier)
        {
            var envSplit = environmentIdentifier.Split('/');

            var envCategory = envSplit[0];
            var envName = envSplit[1];

            return new EnvironmentIdentifier(envCategory, envName);
        }

        private string Reformat(string raw, ReformatKind format)
        {
            try
            {
                var obj = JsonSerializer.Deserialize<ConfigExport>(raw);

                switch (format)
                {
                    case ReformatKind.Compress:
                        return JsonSerializer.Serialize(obj, new JsonSerializerOptions {WriteIndented = false});

                    case ReformatKind.Indent:
                        return JsonSerializer.Serialize(obj, new JsonSerializerOptions {WriteIndented = true});

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