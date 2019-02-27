using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.Utilities.Rest;
using Bechtle.A365.Utilities.Rest.Extensions;
using Bechtle.A365.Utilities.Rest.Receivers;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Cli
{
    [Command("import", Description = "import data to the targeted ConfigService")]
    public class ImportCommand : SubCommand<Program>
    {
        /// <inheritdoc />
        public ImportCommand(IConsole console)
            : base(console)
        {
        }

        [Option("-f|--file")]
        public string File { get; set; }

        /// <inheritdoc />
        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            if (string.IsNullOrWhiteSpace(Parent.ConfigServiceEndpoint))
            {
                LogError($"no {nameof(Parent.ConfigServiceEndpoint)} given -- see help for more information");
                return 1;
            }

            var json = GetInput();
            if (!ValidateInput(json))
            {
                LogError("input failed to validate");
                return 1;
            }

            // name / filename must match parameter-name of ImportController.Import
            var formData = new MultipartFormDataContent {{new ByteArrayContent(Encoding.UTF8.GetBytes(json)), "file", "file"}};

            var request = await RestRequest.Make()
                                           .Post(new Uri(new Uri(Parent.ConfigServiceEndpoint), "import"), formData)
                                           .ReceiveString();

            if (request.HttpResponseMessage?.IsSuccessStatusCode != true)
            {
                LogError($"couldn't import '{File}': {request.HttpResponseMessage?.StatusCode:D} {await request.Take<string>()}");
                return 1;
            }

            return 0;
        }

        private string GetInput()
        {
            if (string.IsNullOrWhiteSpace(File))
            {
                if (Console.IsInputRedirected)
                    return GetInputFromStdIn();

                LogError($"no '{nameof(File)}' parameter given, nothing in stdin");
                return string.Empty;
            }

            return GetInputFromFile(File);
        }

        private string GetInputFromFile(string file)
        {
            if (!System.IO.File.Exists(file))
            {
                LogError($"file '{file}' doesn't seem to exist");
                return string.Empty;
            }

            try
            {
                return System.IO.File.ReadAllText(file, Encoding.UTF8);
            }
            catch (IOException e)
            {
                LogError($"couldn't read file '{file}': {e}");
                return string.Empty;
            }
        }

        private string GetInputFromStdIn()
        {
            if (!Console.IsInputRedirected)
                return string.Empty;
            try
            {
                return Console.In.ReadToEnd();
            }
            catch (Exception e)
            {
                LogError($"couldn't read stdin to end: {e}");
                return string.Empty;
            }
        }

        /// <summary>
        ///     validate the given input against a <see cref="ExportDefinition"/>
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private bool ValidateInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                LogError("no input given");
                return false;
            }

            try
            {
                // validate it against the actual object
                JsonConvert.DeserializeObject<ExportDefinition>(input);
            }
            catch (JsonException e)
            {
                LogError($"couldn't deserialize input: {e}");
                return false;
            }

            return true;
        }
    }
}