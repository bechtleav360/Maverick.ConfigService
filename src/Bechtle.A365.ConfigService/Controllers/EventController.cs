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
                ConfigKeyAction.Set("Endpoints/IdentityService/Name", "identity"),
                ConfigKeyAction.Set("Endpoints/IdentityService/Address", "a365identityservice.a365dev.de"),
                ConfigKeyAction.Set("Endpoints/IdentityService/Port", "44333"),
                ConfigKeyAction.Set("Endpoints/IdentityService/Protocol", "https"),
                ConfigKeyAction.Set("Endpoints/IdentityService/RootPath", ""),
                ConfigKeyAction.Set("LogLevelOverride", "Debug")
            }));

            _store.WriteEvent(new StructureCreated(new StructureIdentifier("AdminService", 1), new[]
            {
                ConfigKeyAction.Set("ClientConfiguration/log_level_override", "{{LogLevelOverride}}"),
                ConfigKeyAction.Set("ClientConfiguration/locale", "de"),
                //ConfigKeyAction.Set("ClientConfiguration/authority", "https://a365identityservice.a365dev.de:44333"),
                ConfigKeyAction.Set("ClientConfiguration/authority",
                                    "{{using:Endpoints/IdentityService; Alias:identity}}{{$identity/protocol}}://{{$identity/address}}:{{$identity/port}}{{$identity/rootPath}}"),
                ConfigKeyAction.Set("ClientConfiguration/Endpoints/ConfigService", "{{Endpoints/ConfigService/*}}"),
                ConfigKeyAction.Set("ClientConfiguration/client_id", "A365.AdminService.Frontend"),
                ConfigKeyAction.Set("ClientConfiguration/redirect_uri", "http://localhost:41764"),
                ConfigKeyAction.Set("ClientConfiguration/response_type", "id_token token"),
                ConfigKeyAction.Set("ClientConfiguration/scope", "openid profile A365.WebApi.Query"),
                ConfigKeyAction.Set("ClientConfiguration/post_logout", "http://localhost:41764"),
            }));

            _store.WriteEvent(new ConfigurationBuilt(new EnvironmentIdentifier("Dev", "Av360"),
                                                     new StructureIdentifier("AdminService", 1)));

            return Ok();
        }
    }
}