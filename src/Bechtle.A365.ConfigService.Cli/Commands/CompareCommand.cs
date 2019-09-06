using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.Utilities.Rest;
using Bechtle.A365.Utilities.Rest.Extensions;
using Bechtle.A365.Utilities.Rest.Receivers;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Command("compare", Description = "compare an exported-environment with one or more local environments")]
    public class CompareCommand : SubCommand<Program>
    {
        public CompareCommand(IConsole console)
            : base(console)
        {
        }

        [Option("-e|--environment", Description = "Environment to export, given in \"{Category}/{Name}\" form")]
        public string[] Environments { get; set; }

        [Option("-i|--input", Description = "location of environment-dump")]
        public string InputFile { get; set; }

        [Option("-o|--output", Description = "location to export data to")]
        public string OutputFile { get; set; }

        [Option("-u|--use-environment", Description = "Environment to use from the ones available in <InputFile> or <stdin> if multiple defined. " +
                                                      "given in \"{Category}/{Name}\" form. " +
                                                      "Can be left out if only one is defined.")]
        public string UseInputEnvironment { get; set; }

        [Option("-m|--mode", Description = "which operations should be recorded to match the target-environment. " +
                                           "'Add' to add keys which are new in source. " +
                                           "'Delete' to remove keys that have been deleted in source. " +
                                           "'Match' to write both 'Add' and 'Delete' operations")]
        public ComparisonMode Mode { get; set; } = ComparisonMode.Match;

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

                return new EnvironmentIdentifier
                {
                    Category = split[0],
                    Name = split[1]
                };
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
        /// <param name="sourceEnvironment"></param>
        /// <param name="targetEnvironments"></param>
        /// <returns></returns>
        private IList<EnvironmentComparison> CompareEnvironments(EnvironmentExport sourceEnvironment,
                                                                 IDictionary<EnvironmentIdentifier, ConfigEnvironmentKey[]> targetEnvironments)
        {
            var comparisons = new List<EnvironmentComparison>();

            try
            {
                foreach (var (id, keys) in targetEnvironments)
                {
                    Output.WriteVerboseLine($"comparing '{sourceEnvironment.Category}/{sourceEnvironment.Name}' <=> '{id}'");

                    var changedKeys = (Mode & ComparisonMode.Add) != 0
                                          ? sourceEnvironment.Keys
                                                             .Where(sk =>
                                                             {
                                                                 // if the given key does not exist in the target environment or is somehow changed
                                                                 // we add it to the list of changed keys for review
                                                                 var foundKey = keys.FirstOrDefault(tk => tk.Key.Equals(sk.Key));
                                                                 var result = foundKey is null
                                                                              || foundKey.Value?.Equals(sk.Value) != true
                                                                              || foundKey.Type?.Equals(sk.Type) != true
                                                                              || foundKey.Description?.Equals(sk.Description) != true;
                                                                 return result;
                                                             })
                                                             .ToList()
                                          : new List<EnvironmentKeyExport>();

                    var deletedKeys = (Mode & ComparisonMode.Delete) != 0
                                          ? sourceEnvironment.Keys
                                                             .Where(sk =>
                                                             {
                                                                 // if any target-key doesn't exist in the source any more,
                                                                 // we add it to the list of deleted keys for review
                                                                 return keys.All(tk => !tk.Key.Equals(sk.Key));
                                                             })
                                                             .ToList()
                                          : new List<EnvironmentKeyExport>();

                    comparisons.Add(new EnvironmentComparison
                    {
                        Source = new EnvironmentIdentifier(sourceEnvironment.Category, sourceEnvironment.Name),
                        Target = id,
                        RequiredActions = changedKeys.Select(c => ConfigKeyAction.Set(c.Key,
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
                foreach (var targetId in envIds)
                {
                    var request = await RestRequest.Make(Output)
                                                   .Get(new Uri(new Uri(ConfigServiceEndpoint), "v1/environments/av360/dev/keys/objects"))
                                                   .ReceiveString()
                                                   .ReceiveObject<List<ConfigEnvironmentKey>>();

                    if (request.HttpResponseMessage?.IsSuccessStatusCode == true)
                        results.Add(targetId, (await request.Take<List<ConfigEnvironmentKey>>())?.ToArray());
                    else
                        Output.WriteErrorLine($"could not retrieve environment '{targetId}' for comparison");
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
                var usedEnvironmentId = new EnvironmentIdentifier
                {
                    Category = useSplit[0],
                    Name = useSplit[1]
                };

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
                export = JsonConvert.DeserializeObject<ConfigExport>(input);

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
        ///     write the result into a file or stdout - depending on <see cref="OutputFile"/>
        /// </summary>
        /// <param name="comparisons"></param>
        /// <returns></returns>
        private async Task<int> WriteResult(IEnumerable<EnvironmentComparison> comparisons)
        {
            try
            {
                var serializer = new JsonSerializer
                {
                    Converters = {new StringEnumConverter()},
                    FloatFormatHandling = FloatFormatHandling.DefaultValue,
                    Formatting = Formatting.Indented
                };

                using (var memoryStream = new MemoryStream())
                using (var textWriter = new StreamWriter(memoryStream, new UTF8Encoding(false)))
                using (var jsonWriter = new JsonTextWriter(textWriter))
                {
                    serializer.Serialize(jsonWriter, comparisons);

                    // flush all data manually to be sure no character is left behind
                    jsonWriter.Flush();

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
                        using (var file = File.OpenWrite(OutputFile))
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
            }
            catch (Exception e)
            {
                Output.WriteLine($"error while writing output: {e}");
                return 1;
            }
        }

        // ReSharper disable once ShiftExpressionRealShiftCountIsZero
        [Flags]
        public enum ComparisonMode : byte
        {
            Add = 1 << 0,
            Delete = 1 << 1,
            Match = Add | Delete
        }
    }
}