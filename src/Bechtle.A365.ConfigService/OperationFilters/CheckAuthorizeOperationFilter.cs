using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Bechtle.A365.ConfigService.OperationFilters
{
    /// <summary>
    ///     Add Authentication-Requirement to Operations if necessary
    /// </summary>
    public class CheckAuthorizeOperationFilter : IOperationFilter
    {
        private readonly ConfigServiceConfiguration _config;

        /// <summary>
        /// </summary>
        /// <param name="configuration"></param>
        public CheckAuthorizeOperationFilter(IConfiguration configuration)
        {
            _config = configuration.Get<ConfigServiceConfiguration>();
        }

        /// <summary>
        ///     apply this filter on given <paramref name="operation"/>
        ///     this will add a requirement for authentication if necessary
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="context"></param>
        public void Apply(Operation operation, OperationFilterContext context)
        {
            // @TODO: search for required authorizations by convention instead of only by Attribute
            //        see Startup for authorization configuration

            // currently authorized access is default, with exceptions
            // return if the method in question allows anonymous access with [AllowAnonymous]
            if (context.MethodInfo
                       .CustomAttributes
                       .Any(attr => attr.AttributeType == typeof(AllowAnonymousAttribute)))
                return;

            if (!operation.Responses.ContainsKey("401"))
                operation.Responses.Add("401", new Response {Description = "Unauthorized"});

            if (!operation.Responses.ContainsKey("403"))
                operation.Responses.Add("403", new Response {Description = "Forbidden"});

            operation.Security = new List<IDictionary<string, IEnumerable<string>>>
            {
                new Dictionary<string, IEnumerable<string>> {{"oauth2", new[] {_config.Authentication.SwaggerScopes}}}
            };
        }
    }
}