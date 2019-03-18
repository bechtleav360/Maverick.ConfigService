using System;
using Bechtle.A365.ConfigService.Common.Converters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     convert Dictionary{string, string} to and from JSON
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "convert")]
    public class ConversionController : V0.ConversionController
    {
        private new const string ApiVersion = "1.0";

        /// <inheritdoc />
        public ConversionController(IServiceProvider provider,
                                    ILogger<ConversionController> logger,
                                    IJsonTranslator translator)
            : base(provider, logger, translator)
        {
        }
    }
}