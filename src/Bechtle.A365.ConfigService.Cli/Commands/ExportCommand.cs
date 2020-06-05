using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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

        [Option("-z|--structure",
                Description = "Structure to export for, given in \"{Name}/{Version}\" form. " +
                              "If set, only Keys used by the given Structures will be exported. " +
                              "Can be given multiple times")]
        public string[] Structures { get; set; }

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

            // Structures may be null or empty - but if not, all entries need to be in correct format
            if (Structures?.Any() == true)
            {
                // if environment doesn't contain '/' or contains multiple of them...
                var errStructures = Structures.Where(s => !s.Contains('/') ||
                                                          s.IndexOf('/') != s.LastIndexOf('/'))
                                              .ToArray();

                // ... complain about them
                if (errStructures.Any())
                {
                    Output.WriteError($"given structures contain errors: {string.Join("; ", errStructures)}");
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            if (!CheckParameters())
                return 1;

            var selectedEnvironments = new List<EnvironmentExportDefinition>(Environments.Length);

            foreach (var environment in Environments)
                try
                {
                    selectedEnvironments.Add(await CreateExportDefinition(environment));
                }
                catch (Exception e)
                {
                    Output.WriteError(e.Message);
                    return 1;
                }

            var exportDefinition = new ExportDefinition {Environments = selectedEnvironments.ToArray()};

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
        private async Task<EnvironmentExportDefinition> CreateExportDefinition(string environmentIdentifier)
        {
            var envSplit = environmentIdentifier.Split('/');

            var envCategory = envSplit[0];
            var envName = envSplit[1];
            var selectedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (Structures?.Any() == true)
            {
                foreach (var structure in Structures)
                {
                    var structSplit = structure.Split('/');
                    var structName = structSplit[0];
                    var structVersion = structSplit[1];

                    var request = await RestRequest.Make(Output)
                                                   .Post(new Uri(new Uri(ConfigServiceEndpoint),
                                                                 $"v1/inspect/structure/compile/{envCategory}/{envName}/{structName}/{structVersion}"),
                                                         new StringContent(string.Empty, Encoding.UTF8))
                                                   .ReceiveObject<StructureInspectionResult>()
                                                   .ReceiveString();

                    var inspectionResult = await request.Take<StructureInspectionResult>();

                    if (inspectionResult is null)
                        throw new Exception($"could not analyze used keys of '{structName}/{structVersion}' in '{envCategory}/{envName}', " +
                                            "no data received from service");

                    if (!inspectionResult.CompilationSuccessful)
                        throw new Exception($"could not analyze used keys of '{structName}/{structVersion}' in '{envCategory}/{envName}', " +
                                            "compilation unsuccessful");

                    foreach (var usedKey in inspectionResult.UsedKeys)
                        selectedKeys.Add(usedKey);
                }
            }

            return new EnvironmentExportDefinition(envCategory, envName, selectedKeys.ToList());
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

        /// <summary>
        ///     Details about the Compilation of a Structure with an Environment
        /// </summary>
        private class StructureInspectionResult
        {
            /// <summary>
            ///     flag indicating if the compilation was successful or not
            /// </summary>
            public bool CompilationSuccessful { get; set; } = false;

            /// <summary>
            ///     resulting compiled configuration
            /// </summary>
            public IDictionary<string, string> CompiledConfiguration { get; set; } = new Dictionary<string, string>();

            /// <summary>
            ///     Path => Error dictionary
            /// </summary>
            public Dictionary<string, List<string>> Errors { get; set; } = new Dictionary<string, List<string>>();

            /// <summary>
            ///     Path => Warning dictionary
            /// </summary>
            public Dictionary<string, List<string>> Warnings { get; set; } = new Dictionary<string, List<string>>();

            /// <summary>
            ///     List of Environment-Keys used to compile this Configuration
            /// </summary>
            public List<string> UsedKeys { get; set; }
        }
    }
}