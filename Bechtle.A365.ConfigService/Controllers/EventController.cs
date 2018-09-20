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
                                                               ConfigKeyAction.Set("Global/Endpoints/AdminService/Address", "localhost"),
                                                               ConfigKeyAction.Set("Global/Endpoints/AdminService/Name", "adminService"),
                                                               ConfigKeyAction.Set("Global/Endpoints/AdminService/Port", "41764"),
                                                               ConfigKeyAction.Set("Global/Endpoints/AdminService/Protocol", "http"),
                                                               ConfigKeyAction.Set("Global/Endpoints/AdminService/RootPath", ""),
                                                           },
                                                           DateTime.UtcNow));

            await _store.WriteEvent(new EnvironmentUpdated("dev",
                                                           new[]
                                                           {
                                                               ConfigKeyAction.Set("Global/Endpoints/ConfigService/Address", "localhost"),
                                                               ConfigKeyAction.Set("Global/Endpoints/ConfigService/Name", "configService"),
                                                               ConfigKeyAction.Set("Global/Endpoints/ConfigService/Port", "5000"),
                                                               ConfigKeyAction.Set("Global/Endpoints/ConfigService/Protocol", "http"),
                                                               ConfigKeyAction.Set("Global/Endpoints/ConfigService/RootPath", ""),
                                                           },
                                                           DateTime.UtcNow));

            await _store.WriteEvent(new SchemaCreated("TestClient",
                                                      new[]
                                                      {
                                                          ConfigKeyAction.Set("Endpoints/AdminService", "[$ENV:Global/Endpoints/AdminService*]"),
                                                          ConfigKeyAction.Set("PollRate", "00:01:00"),
                                                          ConfigKeyAction.Set("EnableCache", "true")
                                                      },
                                                      DateTime.UtcNow));

            await _store.WriteEvent(new SchemaUpdated("TestClient",
                                                      new[]
                                                      {
                                                          ConfigKeyAction.Set("Endpoints/ConfigService",
                                                                              "[using:$ENV/Global/Endpoints/ConfigService][Protocol]://[Address]:[Port][RootPath]"),
                                                      },
                                                      DateTime.UtcNow));

            await _store.WriteEvent(new VersionCompiled("dev", "TestClient", DateTime.UtcNow));

            return Ok();
        }
    }
}