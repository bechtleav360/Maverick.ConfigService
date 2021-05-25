using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ServiceBase.Commands;
using Bechtle.A365.Utilities.Rest;
using Bechtle.A365.Utilities.Rest.Extensions;
using Bechtle.A365.Utilities.Rest.Receivers;
using McMaster.Extensions.CommandLineUtils;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Command("compare", Description = "compare an exported-environment with one or more local environments")]
    [Subcommand(typeof(ImportComparisonCommand))]
    public class CompareCommand : SubCommand<CliBase>
    {
        public CompareCommand(IOutput output) : base(output)
        {
        }

        [Option("-l|--layer", Description = "Layer to compare against the Input-Layer")]
        public string[] Layers { get; set; } = new string[0];

        [Option("-i|--input", Description = "location of layer-dump")]
        public string InputFile { get; set; } = string.Empty;

        [Option("--keep-null-properties", CommandOptionType.NoValue,
                Description = "set this flag to retain null-values if present. otherwise they are replaced with \"\"")]
        public bool KeepNullProperties { get; set; } = false;

        [Option("-m|--mode", Description = "which operations should be executed to match the target-layer. " +
                                           "\n\t\t- 'Add'   : add keys which are new in source. " +
                                           "\n\t\t- 'Delete': remove keys that have been deleted in source. " +
                                           "\n\t\t- 'Match' : execute both 'Add' and 'Delete' operations")]
        public ComparisonMode Mode { get; set; } = ComparisonMode.Match;

        [Option("-o|--output", Description = "location to export data to")]
        public string OutputFile { get; set; } = string.Empty;

        [Option("-u|--use-layer", Description = "Layer to use from the ones available in <InputFile> or <stdin> if multiple defined. " +
                                                "Can be left out if only one is defined.")]
        public string UseInputLayer { get; set; } = string.Empty;

        /// <inheritdoc />
        protected override bool CheckParameters()
        {
            // check Base-Parameters
            if (!base.CheckParameters())
                return false;

            if (Layers is null || !Layers.Any())
            {
                Output.WriteError($"no {nameof(Layers)} given -- see help for more information");
                return false;
            }

            // if environment doesn't contain '/' or contains multiple of them...
            var errEnvironments = Layers.Where(e => !e.Contains('/') ||
                                                    e.IndexOf('/') != e.LastIndexOf('/'))
                                        .ToArray();

            // ... complain about them
            if (errEnvironments.Any())
            {
                Output.WriteError($"given environments contain errors: {string.Join("; ", errEnvironments)}");
                return false;
            }

            // if UseInputLayer is given we have to check for the correct format
            if (!string.IsNullOrWhiteSpace(UseInputLayer)
                && (!UseInputLayer.Contains('/') ||
                    UseInputLayer.IndexOf('/') != UseInputLayer.LastIndexOf('/')))
            {
                Output.WriteError("parameter '-u|--use-environment' is invalid, see 'compare --help' for the required format");
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            if (!CheckParameters())
                return 1;

            var json = GetInput();

            if (!ValidateInput(json, out var export))
            {
                Output.WriteError("input failed to validate");
                return 1;
            }

            var sourceEnvironment = GetExportedEnvironment(export);

            if (sourceEnvironment is null)
            {
                Output.WriteError("no usable Exported Environment could be found");
                return 1;
            }

            var targetEnvironments = await GetLayerKeys(Layers.Select(name => new LayerIdentifier(name)));

            if (targetEnvironments is null)
            {
                Output.WriteError("target environments could not be retrieved for comparison");
                return 1;
            }

            var comparisons = CompareLayers(sourceEnvironment, targetEnvironments);

            if (comparisons is null || !comparisons.Any())
            {
                Output.WriteError("environments could not be compared");
                return 1;
            }

            return await WriteResult(comparisons);
        }

        /// <summary>
        ///     compare the source-env with all target-envs and return the necessary actions to reach each target
        /// </summary>
        /// <param name="targetLayer"></param>
        /// <param name="sourceLayers"></param>
        /// <returns></returns>
        private IList<LayerComparison> CompareLayers(LayerExport targetLayer, IDictionary<LayerIdentifier, DtoConfigKey[]> sourceLayers)
        {
            var comparisons = new List<LayerComparison>();

            try
            {
                foreach (var (id, sourceKeys) in sourceLayers)
                {
                    Output.WriteVerboseLine($"comparing '{targetLayer.Name}' <=> '{id}'");

                    var changedKeys = (Mode & ComparisonMode.Add) != 0
                                          ? targetLayer.Keys.Where(key =>
                                          {
                                              // if the given key does not exist in the target environment or is somehow changed
                                              // we add it to the list of changed keys for review
                                              var sourceKey = sourceKeys.FirstOrDefault(k => k.Key.Equals(key.Key));

                                              // null and "" are treated as equals here
                                              return sourceKey is null
                                                     || !(sourceKey.Value ?? string.Empty).Equals(key.Value ?? string.Empty)
                                                     || !(sourceKey.Type ?? string.Empty).Equals(key.Type ?? string.Empty)
                                                     || !(sourceKey.Description ?? string.Empty).Equals(key.Description ?? string.Empty);
                                          }).ToList()
                                          : new List<EnvironmentKeyExport>();

                    var deletedKeys = (Mode & ComparisonMode.Delete) != 0
                                          ? sourceKeys.Where(sk =>
                                          {
                                              // if any target-key doesn't exist in the source any more,
                                              // we add it to the list of deleted keys for review
                                              return targetLayer.Keys.All(tk => !tk.Key.Equals(sk.Key));
                                          }).ToList()
                                          : new List<DtoConfigKey>();

                    comparisons.Add(new LayerComparison
                    {
                        Source = new LayerIdentifier(targetLayer.Name),
                        Target = id,
                        RequiredActions = changedKeys.Select(c => KeepNullProperties
                                                                      ? ConfigKeyAction.Set(c.Key, c.Value, c.Description, c.Type)
                                                                      : ConfigKeyAction.Set(c.Key,
                                                                                            c.Value ?? string.Empty,
                                                                                            c.Description ?? string.Empty,
                                                                                            c.Type ?? string.Empty))
                                                     .Concat(deletedKeys.Select(d => ConfigKeyAction.Delete(d.Key)))
                                                     .ToList()
                    });
                }
            }
            catch (Exception e)
            {
                Output.WriteLine($"error while comparing layers: {e}");
                return new List<LayerComparison>();
            }

            return comparisons;
        }

        /// <summary>
        ///     retrieve all environment-key-objects for all environment-ids given
        /// </summary>
        /// <param name="envIds"></param>
        /// <returns></returns>
        private async Task<Dictionary<LayerIdentifier, DtoConfigKey[]>> GetLayerKeys(IEnumerable<LayerIdentifier> envIds)
        {
            var results = new Dictionary<LayerIdentifier, DtoConfigKey[]>();

            try
            {
                foreach (var target in envIds)
                {
                    var request = await RestRequest.Make(Output)
                                                   .Get(new Uri(new Uri(ConfigServiceEndpoint),
                                                                $"v1/layers/{target.Name}/objects"))
                                                   .ReceiveString()
                                                   .ReceiveObject<List<DtoConfigKey>>();

                    if (request.HttpResponseMessage?.IsSuccessStatusCode == true)
                        results.Add(target, (await request.Take<List<DtoConfigKey>>())?.ToArray());
                    else
                        Output.WriteErrorLine($"could not retrieve environment '{target}' for comparison");
                }
            }
            catch (Exception e)
            {
                Output.WriteLine($"error while retrieving target-environments: {e}");
                return null;
            }

            return results;
        }

        /// <summary>
        ///     extract the actual desired environment out of the Environment-Export
        /// </summary>
        /// <param name="export"></param>
        /// <returns></returns>
        private LayerExport GetExportedEnvironment(ConfigExport export)
        {
            if (export.Environments.Length > 1)
            {
                if (string.IsNullOrWhiteSpace(UseInputLayer))
                {
                    Output.WriteError("multiple Layers defined in given export, but no '-u|--use-layer' parameter given");
                    return null;
                }

                var usedLayerId = new LayerIdentifier(UseInputLayer);

                return export.Layers
                             .FirstOrDefault(layer => string.Equals(layer.Name, usedLayerId.Name,
                                                                    StringComparison.OrdinalIgnoreCase));
            }

            return export.Layers.FirstOrDefault();
        }

        /// <summary>
        ///     read the input as file or via stdin
        /// </summary>
        /// <returns></returns>
        private string GetInput()
        {
            if (string.IsNullOrWhiteSpace(InputFile))
            {
                if (Output.IsInputRedirected)
                    return GetInputFromStdIn();

                Output.WriteError($"no '{nameof(InputFile)}' parameter given, nothing in stdin");
                return string.Empty;
            }

            return GetInputFromFile(InputFile);
        }

        /// <summary>
        ///     read the input from the given file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private string GetInputFromFile(string file)
        {
            if (!File.Exists(file))
            {
                Output.WriteError($"file '{file}' doesn't seem to exist");
                return string.Empty;
            }

            try
            {
                return File.ReadAllText(file, Encoding.UTF8);
            }
            catch (IOException e)
            {
                Output.WriteError($"couldn't read file '{file}': {e}");
                return string.Empty;
            }
        }

        /// <summary>
        ///     read the input from stdin
        /// </summary>
        /// <returns></returns>
        private string GetInputFromStdIn()
        {
            if (!Output.IsInputRedirected)
                return string.Empty;
            try
            {
                return Console.In.ReadToEnd();
            }
            catch (Exception e)
            {
                Output.WriteError($"couldn't read stdin to end: {e}");
                return string.Empty;
            }
        }

        /// <summary>
        ///     validate the given input against a <see cref="ExportDefinition" />
        /// </summary>
        /// <param name="input"></param>
        /// <param name="export">the actual validated export</param>
        /// <returns></returns>
        private bool ValidateInput(string input, out ConfigExport export)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                Output.WriteError("no input given");
                export = null;
                return false;
            }

            try
            {
                // validate it against the actual object
                export = JsonSerializer.Deserialize<ConfigExport>(input);

                if (export is null)
                {
                    Output.WriteError("couldn't deserialize input: resulting object is null");
                    return false;
                }

                if (export.Environments is null)
                {
                    Output.WriteError("couldn't deserialize input: 'Environments' property is null");
                    return false;
                }

                if (!export.Environments.Any())
                {
                    Output.WriteError("couldn't deserialize input: no Environments defined");
                    return false;
                }

                return true;
            }
            catch (JsonException e)
            {
                Output.WriteError($"couldn't deserialize input: {e}");
                export = null;
                return false;
            }
        }

        /// <summary>
        ///     write the result into a file or stdout - depending on <see cref="OutputFile" />
        /// </summary>
        /// <param name="comparisons"></param>
        /// <returns></returns>
        private async Task<int> WriteResult(IEnumerable<LayerComparison> comparisons)
        {
            try
            {
                await using var memoryStream = new MemoryStream();
                await JsonSerializer.SerializeAsync(memoryStream, comparisons);

                // reset stream to start from beginning next time we read from it
                memoryStream.Position = 0;

                // if no file is given, redirect to stdout
                if (string.IsNullOrWhiteSpace(OutputFile))
                {
                    Output.Write(memoryStream);
                    return 0;
                }

                // if file is given write to it
                try
                {
                    await using (var file = File.OpenWrite(OutputFile))
                    {
                        await memoryStream.CopyToAsync(file);
                    }

                    return 0;
                }
                catch (IOException e)
                {
                    Output.WriteError($"could not write to file '{OutputFile}': {e}");
                    return 1;
                }
            }
            catch (Exception e)
            {
                Output.WriteLine($"error while writing output: {e}");
                return 1;
            }
        }
    }
}