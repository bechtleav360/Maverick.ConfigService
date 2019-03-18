using System;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     preview the Results of Building different Configurations
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "preview")]
    public class PreviewController : V0.PreviewController
    {
        private new const string ApiVersion = "1.0";

        /// <inheritdoc />
        public PreviewController(IServiceProvider provider,
                                 ILogger<PreviewController> logger,
                                 IConfigurationCompiler compiler,
                                 IProjectionStore store,
                                 IConfigurationParser parser,
                                 IJsonTranslator translator)
            : base(provider, logger, compiler, store, parser, translator)
        {
        }
    }
}