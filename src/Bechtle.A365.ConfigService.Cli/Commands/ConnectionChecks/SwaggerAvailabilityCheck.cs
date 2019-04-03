using System;
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
            var swaggerDoc = "swagger/v2/swagger.json";

            var baseUri = new Uri(parameters.ConfigServiceEndpoint);
            var swaggerUri = new Uri(baseUri, swaggerDoc);

            output.WriteLine("Checking OpenAPI");
            output.WriteLine($"using SwaggerDoc @ '{swaggerDoc}'", 1);
            output.WriteLine($"using Url = '{swaggerUri}'", 1);
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

            if (request.HttpResponseMessage?.IsSuccessStatusCode == true)
                return new TestResult
                {
                    Result = true,
                    Message = string.Empty
                };

            if (request.HttpResponseMessage is null)
            {
                return new TestResult
                {
                    Result = false,
                    Message = "no response received"
                };
            }

            return new TestResult
            {
                Result = false,
                Message = $"Reason: {request.HttpResponseMessage.ReasonPhrase}; \r\nResponse: {await request.Take<string>()}"
            };
        }
    }
}