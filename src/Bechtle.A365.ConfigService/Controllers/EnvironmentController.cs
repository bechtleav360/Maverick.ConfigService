using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bechtle.A365.ConfigService.Controllers
{
    /// <summary>
    /// </summary>
    [Route("environments")]
    public class EnvironmentController : Controller
    {
        private readonly IProjectionStore _store;

        /// <inheritdoc />
        public EnvironmentController(IProjectionStore store)
        {
            _store = store;
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableEnvironments()
        {
            var result = await _store.GetAvailableEnvironments();

            return Ok(result);
        }

        [HttpGet("{environmentCategory}/{environmentName}/keys")]
        public async Task<IActionResult> GetEnvironmentKeys(string environmentCategory, string environmentName)
        {
            var identifier = new EnvironmentIdentifier(environmentCategory, environmentName);

            var result = await _store.GetEnvironmentKeys(identifier);

            return Ok(result);
        }
    }
}