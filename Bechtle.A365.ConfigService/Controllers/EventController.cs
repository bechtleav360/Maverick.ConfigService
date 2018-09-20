using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto;
using Bechtle.A365.ConfigService.Dto.EventFactories;
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

        /// <summary>
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> CreateTestData()
        {
            await _store.WriteEvent(EnvironmentCreatedFactory.Build("dev",
                                                                    new[]
                                                                    {
                                                                        ConfigKeyAction.Set("Foo", "Bar"),
                                                                        ConfigKeyAction.Set("Foo/Bar", "42"),
                                                                        ConfigKeyAction.Set("Foo/Baz", "4711"),
                                                                        ConfigKeyAction.Set("Foo/Foo/Oof", "BarBaz")
                                                                    },
                                                                    DateTime.UtcNow));

            await _store.WriteEvent(EnvironmentUpdatedFactory.Build("dev",
                                                                    new[]
                                                                    {
                                                                        ConfigKeyAction.Set("Foo/Foo/Oof", "Chimney"),
                                                                        ConfigKeyAction.Delete("Foo")
                                                                    },
                                                                    DateTime.UtcNow));

            return Ok();
        }
    }
}