using System;
using System.Threading.Tasks;
using Bechtle.A365.Utilities.Rest;
using Bechtle.A365.Utilities.Rest.Extensions;
using Bechtle.A365.Utilities.Rest.Receivers;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Cli.Commands.ConnectionChecks
{
    public class SwaggerAvailabilityCheck : IConnectionCheck
    {
        public string Name => "Swagger.json Availability";

        /// <inheritdoc />
        public async Task<TestResult> Execute(IOutput output, TestParameters parameters, ApplicationSettings settings)
        {
            var swaggerVersionsFound = false;
            var baseUri = new Uri(parameters.ConfigServiceEndpoint);
            var swaggerDocFormat = "swagger/v{0}/swagger.json";

            output.WriteLine("Checking OpenAPI");
            output.WriteLine($"Using SwaggerDoc @ '{swaggerDocFormat}'");

            var currentSwaggerVersion = 0;

            output.WriteLine("Swagger-Versions identified:", 1);
            do
            {
                var swaggerDoc = string.Format(swaggerDocFormat, currentSwaggerVersion);
                var swaggerUri = new Uri(baseUri, swaggerDoc);

                var result = await CheckSwaggerDoc(output, swaggerUri);

                if (result.Result)
                {
                    swaggerVersionsFound = true;
                    output.WriteLine($"V{currentSwaggerVersion,2:#0}; {swaggerUri}", 2);
                    currentSwaggerVersion++;
                }
                else
                    break;
            } while (true);

            if (swaggerVersionsFound)
                return new TestResult
                {
                    Result = true,
                    Message = string.Empty
                };

            output.WriteLine("---", 2);

            return new TestResult
            {
                Result = false,
                Message = ""
            };
        }

        private async Task<TestResult> CheckSwaggerDoc(ILogger logger, Uri swaggerUri)
        {
            var request = await RestRequest.Make(logger)
                                           .Get(swaggerUri)
                                           .ReceiveString();

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
                           Message = $"Reason: {request.HttpResponseMessage.ReasonPhrase};" +
                                     $"\r\nResponse: {await request.Take<string>()}"
                       };
        }
    }
}