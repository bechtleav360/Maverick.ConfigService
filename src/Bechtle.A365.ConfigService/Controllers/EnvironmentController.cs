using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers
{
    /// <summary>
    /// </summary>
    [Route(ApiBaseRoute + "environments")]
    public class EnvironmentController : ControllerBase
    {
        private readonly IProjectionStore _store;

        /// <inheritdoc />
        public EnvironmentController(IServiceProvider provider,
                                     ILogger<EnvironmentController> logger,
                                     IProjectionStore store)
            : base(provider, logger)
        {
            _store = store;
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableEnvironments()
        {
            var result = await _store.Environments.GetAvailable();

            return Result(result);
        }

        [HttpGet("{environmentCategory}/{environmentName}/keys")]
        public async Task<IActionResult> GetEnvironmentKeys(string environmentCategory, string environmentName)
        {
            var identifier = new EnvironmentIdentifier(environmentCategory, environmentName);

            var result = await _store.Environments.GetKeys(identifier);

            return Result(result);
        }
    }
}