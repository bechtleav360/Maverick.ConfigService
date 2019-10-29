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
using Bechtle.A365.Utilities.Rest;
using Bechtle.A365.Utilities.Rest.Extensions;
using Bechtle.A365.Utilities.Rest.Receivers;
using McMaster.Extensions.CommandLineUtils;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Command("compare", Description = "compare an exported-environment with one or more local environments")]
    [Subcommand(typeof(ImportComparisonCommand))]
    public class CompareCommand : SubCommand<Program>
    {
        public CompareCommand(IConsole console)
            : base(console)
        {
        }

        [Option("-e|--environment", Description = "Environment to compare against the Input-Environment, given in \"{Category}/{Name}\" form")]
        public string[] Environments { get; set; } = new string[0];

        [Option("-i|--input", Description = "location of environment-dump")]
        public string InputFile { get; set; } = string.Empty;

        [Option("--keep-null-properties", CommandOptionType.NoValue,
            Description = "set this flag to retain null-values if present. otherwise they are replaced with \"\"")]
        public bool KeepNullProperties { get; set; } = false;

        [Option("-m|--mode", Description = "which operations should be executed to match the target-environment. " +
                                           "\n\t\t- 'Add'   : add keys which are new in source. " +
                                           "\n\t\t- 'Delete': remove keys that have been deleted in source. " +
                                           "\n\t\t- 'Match' : execute both 'Add' and 'Delete' operations")]
        public ComparisonMode Mode { get; set; } = ComparisonMode.Match;

        [Option("-o|--output", Description = "location to export data to")]
        public string OutputFile { get; set; } = string.Empty;

        [Option("-u|--use-environment", Description = "Environment to use from the ones available in <InputFile> or <stdin> if multiple defined. " +
                                                      "given in \"{Category}/{Name}\" form. " +
                                                      "Can be left out if only one is defined.")]
        public string UseInputEnvironment { get; set; } = string.Empty;

        /// <inheritdoc />
        protected override bool CheckParameters()
        {
            // check Base-Parameters
            if (!base.CheckParameters())
                return false;

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

            // if UseInputEnvironment is given we have to check for the correct format
            if (!string.IsNullOrWhiteSpace(UseInputEnvironment)
                && (!UseInputEnvironment.Contains('/') ||
                    UseInputEnvironment.IndexOf('/') != UseInputEnvironment.LastIndexOf('/')))
            {
                Output.WriteError("parameter '-u|--use-environment' is invalid, see 'compare --help' for the required format");
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        protected override async Task<int> OnExecute(CommandLineApplication app)
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

            var targetEnvironments = await GetEnvironmentKeys(Environments.Select(e =>
            {
                var split = e.Split('/');

                return new EnvironmentIdentifier(split[0], split[1]);
            }));

            if (targetEnvironments is null)
            {
                Output.WriteError("target environments could not be retrieved for comparison");
                return 1;
            }

            var comparisons = CompareEnvironments(sourceEnvironment, targetEnvironments);

            if (comparisons is null)
            {
                Output.WriteError("environments could not be compared");
                return 1;
            }

            return await WriteResult(comparisons);
        }

        /// <summary>
        ///     compare the source-env with all target-envs and return the necessary actions to reach each target
        /// </summary>
        /// <param name="targetEnvironment"></param>
        /// <param name="sourceEnvironments"></param>
        /// <returns></returns>
        private IList<EnvironmentComparison> CompareEnvironments(EnvironmentExport targetEnvironment,
                                                                 IDictionary<EnvironmentIdentifier, ConfigEnvironmentKey[]> sourceEnvironments)
        {
            var comparisons = new List<EnvironmentComparison>();

            try
            {
                foreach (var (id, sourceKeys) in sourceEnvironments)
                {
                    Output.WriteVerboseLine($"comparing '{targetEnvironment.Category}/{targetEnvironment.Name}' <=> '{id}'");

                    var changedKeys = (Mode & ComparisonMode.Add) != 0
                                          ? targetEnvironment.Keys.Where(key =>
                                          {
                                              // if the given key does not exist in the target environment or is somehow changed
                                              // we add it to the list of changed keys for review
                                              var sourceKey = sourceKeys.FirstOrDefault(k => k.Key.Equals(key.Key));

                                              // null and "" are treated as equals here
                                              return sourceKey is null
                                                     || (sourceKey.Value ?? string.Empty).Equals(key.Value ?? string.Empty) != true
                                                     || (sourceKey.Type ?? string.Empty).Equals(key.Type ?? string.Empty) != true
                                                     || (sourceKey.Description ?? string.Empty).Equals(key.Description ?? string.Empty) != true;
                                          }).ToList()
                                          : new List<EnvironmentKeyExport>();

                    var deletedKeys = (Mode & ComparisonMode.Delete) != 0
                                          ? sourceKeys.Where(sk =>
                                          {
                                              // if any target-key doesn't exist in the source any more,
                                              // we add it to the list of deleted keys for review
                                              return targetEnvironment.Keys.All(tk => !tk.Key.Equals(sk.Key));
                                          }).ToList()
                                          : new List<ConfigEnvironmentKey>();

                    comparisons.Add(new EnvironmentComparison
                    {
                        Source = new EnvironmentIdentifier(targetEnvironment.Category, targetEnvironment.Name),
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
                Output.WriteLine($"error while comparing environments: {e}");
                return null;
            }

            return comparisons;
        }

        /// <summary>
        ///     retrieve all environment-key-objects for all environment-ids given
        /// </summary>
        /// <param name="envIds"></param>
        /// <returns></returns>
        private async Task<Dictionary<EnvironmentIdentifier, ConfigEnvironmentKey[]>> GetEnvironmentKeys(IEnumerable<EnvironmentIdentifier> envIds)
        {
            var results = new Dictionary<EnvironmentIdentifier, ConfigEnvironmentKey[]>();

            try
            {
                foreach (var target in envIds)
                {
                    var request = await RestRequest.Make(Output)
                                                   .Get(new Uri(new Uri(ConfigServiceEndpoint),
                                                                $"v1/environments/{target.Category}/{target.Name}/keys/objects"))
                                                   .ReceiveString()
                                                   .ReceiveObject<List<ConfigEnvironmentKey>>();

                    if (request.HttpResponseMessage?.IsSuccessStatusCode == true)
                        results.Add(target, (await request.Take<List<ConfigEnvironmentKey>>())?.ToArray());
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
        private EnvironmentExport GetExportedEnvironment(ConfigExport export)
        {
            if (export.Environments.Length > 1)
            {
                if (string.IsNullOrWhiteSpace(UseInputEnvironment))
                {
                    Output.WriteError("multiple Environments defined in given export, but no '-u|--use-environment' parameter given");
                    return null;
                }

                var useSplit = UseInputEnvironment.Split('/');
                var usedEnvironmentId = new EnvironmentIdentifier(useSplit[0], useSplit[1]);

                return export.Environments
                             .FirstOrDefault(e => string.Equals(e.Category,
                                                                usedEnvironmentId.Category,
                                                                StringComparison.OrdinalIgnoreCase)
                                                  && string.Equals(e.Name,
                                                                   usedEnvironmentId.Name,
                                                                   StringComparison.OrdinalIgnoreCase));
            }

            return export.Environments.FirstOrDefault();
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
        private async Task<int> WriteResult(IEnumerable<EnvironmentComparison> comparisons)
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