using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ServiceBase.Commands;
using Bechtle.A365.Utilities.Rest;
using Bechtle.A365.Utilities.Rest.Extensions;
using Bechtle.A365.Utilities.Rest.Receivers;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Command("import", Description = "import data to the targeted ConfigService")]
    public class ImportCommand : SubCommand<CliBase>
    {
        /// <inheritdoc />
        public ImportCommand(IOutput output) : base(output)
        {
        }

        [Option("-f|--file", Description = "Config-Exports created with the 'export' command, or directly from ConfigService. \r\n" +
                                           "Multiple files each with a single Environment can be given, " +
                                           "they will overwrite their data in the order they're given.\r\n" +
                                           "If the first file contains multiple Environments, overwriting keys is not possible.\r\n" +
                                           "If subsequent files contain multiple Environments, none are used to overwrite data.")]
        public string File { get; set; }

        /// <inheritdoc />
        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
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
                Output.WriteError($"couldn't import '{string.Join(", ", File)}': {request.HttpResponseMessage?.StatusCode:D} {await request.Take<string>()}");
                return 1;
            }

            return 0;
        }

        private ConfigExport GetInput()
        {
            if (string.IsNullOrWhiteSpace(File))
            {
                if (Output.IsInputRedirected)
                    return GetInputFromStdIn();

                Output.WriteError($"no '{nameof(System.IO.File)}' parameter given, nothing in stdin");
                return null;
            }

            return GetInputFromFile(File);
        }

        private ConfigExport GetInputFromFile(string file)
        {
            ConfigExport result = null;

            try
            {
                if (System.IO.File.Exists(file))
                    result = JsonConvert.DeserializeObject<ConfigExport>(System.IO.File.ReadAllText(file, Encoding.UTF8));
                else
                    Output.WriteError($"file '{file}' doesn't seem to exist");
            }
            catch (JsonException e)
            {
                Output.WriteError($"couldn't deserialize file '{file}': {e}");
            }
            catch (IOException e)
            {
                Output.WriteError($"couldn't read file '{file}': {e}");
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