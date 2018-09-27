using Bechtle.A365.ConfigService.Dto;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bechtle.A365.ConfigService.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [Route("events")]
    public class EventController : Controller
    {
        private readonly IConfigStore _store;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="store"></param>
        public EventController(IConfigStore store)
        {
            _store = store;
        }

        [HttpPost]
        public IActionResult TestPost()
        {
            _store.WriteEvent(new EnvironmentCreated(new EnvironmentIdentifier("Dev", "Av360")));

            _store.WriteEvent(new EnvironmentKeysModified(new EnvironmentIdentifier("Dev", "Av360"), new[]
            {
                ConfigKeyAction.Set("Endpoints/ConfigService/Name", "configuration"),
                ConfigKeyAction.Set("Endpoints/ConfigService/Address", "localhost"),
                ConfigKeyAction.Set("Endpoints/ConfigService/Port", "80"),
                ConfigKeyAction.Set("Endpoints/ConfigService/Protocol", "http"),
                ConfigKeyAction.Set("Endpoints/ConfigService/RootPath", ""),
            }));

            _store.WriteEvent(new StructureCreated(new StructureIdentifier("configuration", 1)));

            return Ok();
        }
    }
}