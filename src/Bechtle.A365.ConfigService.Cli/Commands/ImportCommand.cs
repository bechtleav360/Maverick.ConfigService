using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.Utilities.Rest;
using Bechtle.A365.Utilities.Rest.Extensions;
using Bechtle.A365.Utilities.Rest.Receivers;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Command("import", Description = "import data to the targeted ConfigService")]
    public class ImportCommand : SubCommand<Program>
    {
        /// <inheritdoc />
        public ImportCommand(IConsole console)
            : base(console)
        {
        }

        [Option("-f|--files", Description = "Config-Exports created with the 'export' command, or directly from ConfigService. \r\n" +
                                            "Multiple files each with a single Environment can be given, " +
                                            "they will overwrite their data in the order they're given.\r\n" +
                                            "If the first file contains multiple Environments, overwriting keys is not possible.\r\n" +
                                            "If subsequent files contain multiple Environments, none are used to overwrite data.")]
        public string[] Files { get; set; }

        /// <inheritdoc />
        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            if (!CheckParameters())
                return 1;

            var export = GetInput();
            if (export is null)
            {
                Output.WriteError("input failed to validate");
                return 1;
            }

            var utf8Bytes = JsonSerializer.SerializeToUtf8Bytes(export);

            // name / filename must match parameter-name of ImportController.Import
            var formData = new MultipartFormDataContent {{new ByteArrayContent(utf8Bytes), "file", "file"}};

            var request = await RestRequest.Make(Output)
                                           .Post(new Uri(new Uri(ConfigServiceEndpoint), "v1/import"), formData)
                                           .ReceiveString();

            if (request.HttpResponseMessage?.IsSuccessStatusCode != true)
            {
                Output.WriteError($"couldn't import '{string.Join(", ", Files)}': {request.HttpResponseMessage?.StatusCode:D} {await request.Take<string>()}");
                return 1;
            }

            return 0;
        }

        private ConfigExport GetInput()
        {
            if (Files is null || !Files.Any())
            {
                if (Output.IsInputRedirected)
                    return GetInputFromStdIn();

                Output.WriteError($"no '{nameof(File)}' parameter given, nothing in stdin");
                return null;
            }

            return GetInputFromFiles(Files);
        }

        private ConfigExport GetInputFromFiles(string[] files)
        {
            ConfigExport result = null;

            foreach (var file in files)
            {
                if (!File.Exists(file))
                {
                    Output.WriteError($"file '{file}' doesn't seem to exist");
                    continue;
                }

                try
                {
                    var content = File.ReadAllText(file, Encoding.UTF8);
                    var export = JsonConvert.DeserializeObject<ConfigExport>(content);

                    if (result is null)
                    {
                        result = export;

                        // if there is nothing in here, we might as well assume we didn't get anything
                        if (result?.Environments is null || !result.Environments.Any())
                            result = null;
                        // if we get more than one environment here, we can't safely overwrite anything with the other files, so we skip'em
                        else if (result.Environments.Length > 1)
                            return result;
                    }
                    else
                    {
                        if (export.Environments.Length == 0)
                        {
                            Output.WriteErrorLine($"no environment found in '{file}' - skipping");
                            continue;
                        }

                        if (export.Environments.Length > 1)
                        {
                            Output.WriteErrorLine($"multiple environments found in '{file}' - skipping");
                            continue;
                        }

                        var definition = export.Environments.First();

                        if (!string.IsNullOrWhiteSpace(definition.Category))
                            result.Environments[0].Category = definition.Category;

                        if (!string.IsNullOrWhiteSpace(definition.Name))
                            result.Environments[0].Name = definition.Name;

                        // look for same Paths and overwrite with newer data
                        if (!(definition.Keys is null) && definition.Keys.Any())
                        {
                            var newKeys = new List<EnvironmentKeyExport>();

                            foreach (var key in definition.Keys)
                            {
                                // search vor keys to overwrite, case-insensitive
                                var existing = result.Environments[0]
                                                     .Keys
                                                     .FirstOrDefault(k => string.Equals(k.Key,
                                                                                        key.Key,
                                                                                        StringComparison.OrdinalIgnoreCase));

                                if (existing is null)
                                {
                                    newKeys.Add(key);
                                    continue;
                                }

                                existing.Key = key.Key;
                                existing.Description = key.Description;
                                existing.Type = key.Description;
                                existing.Value = key.Value;
                            }

                            result.Environments[0].Keys = result.Environments[0]
                                                                .Keys
                                                                .Concat(newKeys)
                                                                .ToArray();
                        }
                    }
                }
                catch (JsonException e)
                {
                    Output.WriteError($"couldn't deserialize file '{file}': {e}");
                }
                catch (IOException e)
                {
                    Output.WriteError($"couldn't read file '{file}': {e}");
                }
            }

            return result;
        }

        private ConfigExport GetInputFromStdIn()
        {
            if (!Output.IsInputRedirected)
                return null;
            try
            {
                return JsonConvert.DeserializeObject<ConfigExport>(Console.In.ReadToEnd());
            }
            catch (Exception e)
            {
                Output.WriteError($"couldn't read stdin to end: {e}");
                return null;
            }
        }
    }
}