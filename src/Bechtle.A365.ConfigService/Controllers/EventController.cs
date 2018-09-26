using System;
using System.Threading.Tasks;
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

        /// <summary>
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> CreateTestData()
        {
            await _store.WriteEvent(new EnvironmentCreated("dev",
                                                           new[]
                                                           {
                                                               ConfigKeyAction.Set("Endpoints/AdminService/Address", "localhost"),
                                                               ConfigKeyAction.Set("Endpoints/AdminService/Name", "adminService"),
                                                               ConfigKeyAction.Set("Endpoints/AdminService/Port", "41764"),
                                                               ConfigKeyAction.Set("Endpoints/AdminService/Protocol", "http"),
                                                               ConfigKeyAction.Set("Endpoints/AdminService/RootPath", ""),
                                                           },
                                                           DateTime.UtcNow));

            await _store.WriteEvent(new EnvironmentUpdated("dev",
                                                           new[]
                                                           {
                                                               ConfigKeyAction.Set("Endpoints/ConfigService/Address", "localhost"),
                                                               ConfigKeyAction.Set("Endpoints/ConfigService/Name", "configService"),
                                                               ConfigKeyAction.Set("Endpoints/ConfigService/Port", "5000"),
                                                               ConfigKeyAction.Set("Endpoints/ConfigService/Protocol", "http"),
                                                               ConfigKeyAction.Set("Endpoints/ConfigService/RootPath", ""),
                                                           },
                                                           DateTime.UtcNow));

            await _store.WriteEvent(new SchemaCreated("TestClient",
                                                      new[]
                                                      {
                                                          ConfigKeyAction.Set("Endpoints/AdminService", "[$ENV/Endpoints/AdminService/Name]"),
                                                          ConfigKeyAction.Set("PollRate", "00:01:00"),
                                                          ConfigKeyAction.Set("EnableCache", "true")
                                                      },
                                                      DateTime.UtcNow));

            await _store.WriteEvent(new VersionCompiled("dev", "TestClient", DateTime.UtcNow));

            return Ok();
        }
    }
}