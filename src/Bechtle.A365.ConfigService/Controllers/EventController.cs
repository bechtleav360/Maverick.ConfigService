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
    }
}