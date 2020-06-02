using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.Utilities.Rest;
using Bechtle.A365.Utilities.Rest.Extensions;
using Bechtle.A365.Utilities.Rest.Receivers;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Command("unused-keys", Description = "browse environment-keys that are not used in by structures")]
    public class BrowseUnusedEnvironmentKeysCommand : SubCommand<BrowseCommand>
    {
        private readonly HttpClient _httpClient;

        /// <inheritdoc />
        public BrowseUnusedEnvironmentKeysCommand(IConsole console, IHttpClientFactory httpClientFactory) : base(console)
        {
            _httpClient = httpClientFactory.CreateClient(nameof(BrowseUnusedEnvironmentKeysCommand));
        }

        [Option("-e|--environment", "use the given environment for comparison, given in \"{Category}/{Name}\" form", CommandOptionType.SingleValue)]
        public string Environment { get; set; }

        [Option("-u|--hide-used", "hide all keys that are used by at least one Structure", CommandOptionType.NoValue)]
        public bool HideUsedKeys { get; set; } = false;

        [Option("-d|--display", "set the display-mode to either Table or Json", CommandOptionType.SingleValue)]
        public DisplayType Display { get; set; } = DisplayType.Table;

        protected override bool CheckParameters()
        {
            // check Base-Parameters
            if (!base.CheckParameters())
                return false;

            if (string.IsNullOrWhiteSpace(Environment))
            {
                Output.WriteError($"no {nameof(Environment)} given -- see help for more information");
                return false;
            }

            // if environment doesn't contain '/' or contains multiple of them...
            if (!Environment.Contains('/') || Environment.IndexOf('/') != Environment.LastIndexOf('/'))
            {
                Output.WriteError("Environment must contain exactly one '/' to be valid - \"{Category}/{Name}\"");
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var split = Environment.Split('/', 2);
            var envCategory = split[0];
            var envName = split[1];

            var request = await RestRequest.Make(Output)
                                           .Post(new Uri(new Uri(ConfigServiceEndpoint), $"v1/inspect/environment/{envCategory}/{envName}/structures/latest"),
                                                 new StringContent(string.Empty, Encoding.UTF8),
                                                 _httpClient)
                                           .ReceiveObject<AnnotatedEnvironmentKey[]>()
                                           .ReceiveString();

            if (request.HttpResponseMessage?.IsSuccessStatusCode == true)
            {
                var environments = await request.Take<AnnotatedEnvironmentKey[]>();

                if (environments is null)
                {
                    Output.WriteErrorLine($"couldn't query unused keys for environment '{envCategory}/{envName}': " +
                                          $"{request.HttpResponseMessage?.StatusCode:D} " +
                                          $"({request.HttpResponseMessage?.StatusCode:G}); " +
                                          "can't convert response to target type");

                    var rawResponse = await request.Take<string>();
                    Output.WriteErrorLine($"couldn't deserialize response: {rawResponse}");

                    return 1;
                }

                var results = environments.OrderBy(e => e.Key)
                                          .Where(e => !e.Structures.Any() || !HideUsedKeys)
                                          .ToList();

                if (Display == DisplayType.Table)
                {
                    Output.WriteTable(results,
                                      e => new Dictionary<string, object>
                                      {
                                          {"Key", e.Key},
                                          {"Structures (Count)", e.Structures.Count},
                                          {"Value (Length)", $"{e.Value?.Length.ToString() ?? "null"}"}
                                      },
                                      new Dictionary<string, TextAlign>
                                      {
                                          {"Key", TextAlign.Left},
                                          {"Structures (Count)", TextAlign.Center},
                                          {"Value (Length)", TextAlign.Right}
                                      });

                    if (request.HttpResponseMessage.Headers.TryGetValues("x-omitted-configs", out var headerValues)
                        && headerValues.First() is { } headerValue)
                    {
                        var omittedConfigs = headerValue.Split(";");

                        Output.WriteTable(omittedConfigs.Select(oc =>
                        {
                            var s = oc.Split("/", 2);
                            return new {Category = s[0], Name = s[1]};
                        }), o => new Dictionary<string, object>
                        {
                            {"Category", o.Category},
                            {"Name", o.Name},
                        }, new Dictionary<string, TextAlign>
                        {
                            {"Category", TextAlign.Left},
                            {"Name", TextAlign.Left},
                        });
                    }
                }
                else
                {
                    Output.Write(JsonConvert.SerializeObject(results.Select(e => e.Key)));
                }

                return 0;
            }

            Output.WriteErrorLine($"couldn't query unused keys for environment '{envCategory}/{envName}': " +
                                  $"{request.HttpResponseMessage?.StatusCode:D} " +
                                  $"({request.HttpResponseMessage?.StatusCode:G})");

            var response = await request.Take<string>();
            Output.WriteErrorLine(response ?? "{no response received}");

            return 1;
        }

        /// <summary>
        ///     Environment-data annotated with a list of structures for each key
        /// </summary>
        public class AnnotatedEnvironmentKey
        {
            /// <summary>
            ///     Environment-Key
            /// </summary>
            public string Key { get; set; } = string.Empty;

            /// <summary>
            ///     List of Structures that used this Key
            /// </summary>
            public List<StructureIdentifier> Structures { get; set; } = new List<StructureIdentifier>();

            /// <summary>
            ///     Current Value
            /// </summary>
            public string Value { get; set; } = string.Empty;
        }

        public enum DisplayType
        {
            Table,
            Json
        }
    }
}