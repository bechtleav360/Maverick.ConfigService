﻿using System;
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
        public async Task<TestResult> Execute(FormattedOutput output, TestParameters parameters)
        {
            var swaggerDoc = "swagger/v2/swagger.json";

            var baseUri = new Uri(parameters.ConfigServiceEndpoint);
            var swaggerUri = new Uri(baseUri, swaggerDoc);

            output.Line("Checking OpenAPI");
            output.Line($"using SwaggerDoc @ '{swaggerDoc}'", 1);
            output.Line($"using Url = '{swaggerUri}'", 1);
            output.Line(1);

            var request = await RestRequest.Make(output.Logger)
                                           .Get(swaggerUri)
                                           .ReceiveString();

            var statusCode = request.HttpResponseMessage?.StatusCode;
            var result = await request.Take<string>();

            if (request.HttpResponseMessage?.Headers is null)
                output.Line("Headers: -null-", 1);
            else if (!request.HttpResponseMessage.Headers.Any())
                output.Line("Headers: -empty-", 1);
            else
            {
                output.Line("Headers:", 1);

                foreach (var (key, values) in request.HttpResponseMessage.Headers)
                foreach (var value in values)
                    output.Line($"{key} = {value}", 2);
            }

            output.Line(1);
            output.Line($"StatusCode: {statusCode:G} {statusCode:D}", 1);
            output.Line($"Result: {result.Length}", 1);
            output.Line($"Result: {result}", 1, true);
            output.Line(1);

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