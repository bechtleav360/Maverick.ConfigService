using System.Threading.Tasks;
using Bechtle.A365.ConfigService.DomainEvents;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bechtle.A365.ConfigService.Controllers
{
    [Route("events")]
    public class EventController : Controller
    {
        private readonly IConfigStore _store;

        public EventController(IConfigStore store)
        {
            _store = store;
        }

        [HttpGet]
        public ActionResult<DomainEvent[]> GetAll()
        {
            var result = _store.GetAll();

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult> CreateDummy()
        {
            await _store.WriteEvent(new DummyEvent());

            return Ok();
        }
    }
}