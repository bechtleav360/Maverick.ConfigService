using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.Utilities.Rest;
using Bechtle.A365.Utilities.Rest.Extensions;
using Bechtle.A365.Utilities.Rest.Receivers;

namespace Bechtle.A365.ConfigService.Cli.Commands.ConnectionChecks
{
    public class SwaggerAvailabilityCheck : IConnectionCheck
    {
        public string Name => "Swagger.json Availability";

        /// <inheritdoc />
        public async Task<TestResult> Execute(IOutput output, TestParameters parameters, ApplicationSettings settings)
        {
            var baseUri = new Uri(parameters.ConfigServiceEndpoint);
            var swaggerDocFormat = "swagger/v{0}/swagger.json";

            output.WriteLine("Checking OpenAPI");
            output.WriteLine($"Using SwaggerDoc @ '{swaggerDocFormat}'");

            var currentSwaggerVersion = 1;
            var swaggerVersionsFound = new List<int>();

            do
            {
                var result = await CheckSwaggerDoc(output, baseUri, string.Format(swaggerDocFormat, currentSwaggerVersion));

                if (result.Result)
                {
                    swaggerVersionsFound.Add(currentSwaggerVersion);
                    currentSwaggerVersion += 1;
                }
                else
                    break;
            } while (true);

            if (swaggerVersionsFound.Any())
            {
                output.WriteLine("Swagger-Versions identified:", 1);
                foreach (var v in swaggerVersionsFound.OrderBy(x => x))
                    output.WriteLine($"V {v}", 2);

                return new TestResult
                {
                    Result = true,
                    Message = string.Empty
                };
            }

            output.WriteLine("No Swagger-Version identified", 1);

            return new TestResult
            {
                Result = false,
                Message = ""
            };
        }

        private async Task<TestResult> CheckSwaggerDoc(IOutput output, Uri baseUri, string swaggerDoc)
        {
            var swaggerUri = new Uri(baseUri, swaggerDoc);

            output.WriteLine(string.Empty, 1);
            output.WriteLine($"Using SwaggerDoc @ '{swaggerDoc}'", 1);
            output.WriteLine($"Using Url = '{swaggerUri}'", 1);
            output.WriteLine(string.Empty, 1);

            var request = await RestRequest.Make(output)
                                           .Get(swaggerUri)
                                           .ReceiveString();

            var statusCode = request.HttpResponseMessage?.StatusCode;
            var result = await request.Take<string>();

            if (request.HttpResponseMessage?.Headers is null)
                output.WriteLine("Headers: -null-", 1);
            else if (!request.HttpResponseMessage.Headers.Any())
                output.WriteLine("Headers: -empty-", 1);
            else
            {
                output.WriteLine("Headers:", 1);

                foreach (var (key, values) in request.HttpResponseMessage.Headers)
                foreach (var value in values)
                    output.WriteLine($"{key} = {value}", 2);
            }

            output.WriteLine(string.Empty, 1);
            output.WriteLine($"StatusCode: {statusCode:G} {statusCode:D}", 1);
            output.WriteLine($"Result: {result.Length}", 1);
            output.WriteVerboseLine($"Result: {result}", 1);
            output.WriteLine(string.Empty, 1);

            if (request.HttpResponseMessage is null)
                return new TestResult
                {
                    Result = false,
                    Message = "no response received"
                };

            return request.HttpResponseMessage.IsSuccessStatusCode
                       ? new TestResult
                       {
                           Result = true,
                           Message = string.Empty
                       }
                       : new TestResult
                       {
                           Result = false,
                           Message = $"Reason: {request.HttpResponseMessage.ReasonPhrase}; \r\nResponse: {await request.Take<string>()}"
                       };
        }
    }
}