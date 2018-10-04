using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bechtle.A365.ConfigService.Controllers
{
    [Route("structures")]
    public class StructureController : Controller
    {
        private readonly IProjectionStore _store;

        /// <inheritdoc />
        public StructureController(IProjectionStore store)
        {
            _store = store;
        }
        
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableStructures()
        {
            var result = await _store.GetAvailableStructures();

            return Ok(result);
        }

        [HttpGet("{name}/{version}/keys")]
        public async Task<IActionResult> GetStructureKeys(string name, int version)
        {
            var identifier = new StructureIdentifier(name, version);

            var result = await _store.GetStructureKeys(identifier);

            return Ok(result);
        }
    }
}