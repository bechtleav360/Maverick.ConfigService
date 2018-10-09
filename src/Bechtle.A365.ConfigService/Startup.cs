using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Bechtle.A365.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.EventFactories;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Extensions;
using Bechtle.A365.ConfigService.OperationFilters;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using ESLogger = EventStore.ClientAPI.ILogger;

namespace Bechtle.A365.ConfigService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var config = Configuration.Get<ConfigServiceConfiguration>();

            var authorityEndpoint = config.IndexedEndpoints.ContainsKey(WellKnownEndpoints.IdentityService)
                                        ? config.IndexedEndpoints[WellKnownEndpoints.IdentityService]
                                        : null;

            if (authorityEndpoint == null)
                throw new Exception($"no endpoint found for '{WellKnownEndpoints.IdentityService}'");

            var authorityUri = authorityEndpoint.ToUri();

            // setup MVC
            services.AddMvc(options =>
                    {
                        // require authorization by default
                        options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
                    })
                    .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // setup Swagger and Swagger-OAuth
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v2", new Info
                {
                    Title = "Bechtle.A365.ConfigService",
                    Version = "V2.0"
                });
                options.DescribeAllEnumsAsStrings();
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetEntryAssembly().GetName().Name}.xml"));
                options.AddSecurityDefinition("oauth2", new OAuth2Scheme
                {
                    // Urls require '/connect/[authorize|token]'
                    AuthorizationUrl = new Uri(authorityUri, "connect/authorize").ToString(),
                    TokenUrl = new Uri(authorityUri, "connect/token").ToString(),
                    Flow = "implicit",
                    // must include resource scopes, but no identity scopes.
                    Scopes = new Dictionary<string, string> {{config.Authentication.SwaggerScopes, "A365 Identity Scopes"}}
                });
                options.OperationFilter<CheckAuthorizeOperationFilter>();
            });

            // setup Authentication
            services.AddAuthentication("Bearer")
                    .AddIdentityServerAuthentication(options =>
                    {
                        options.Authority = authorityUri.ToString();
                        options.RequireHttpsMetadata = true;

                        options.ApiName = config.Authentication.ApiResourceName;
                        options.ApiSecret = config.Authentication.ApiResourceSecret;
                    });

            // setup services for DI
            services.AddEntityFrameworkProxies()
                    .AddSingleton(provider => provider.GetService<IConfiguration>().Get<ConfigServiceConfiguration>())
                    .AddSingleton(provider => provider.GetService<ConfigServiceConfiguration>().EventStoreConnection)
                    .AddSingleton(provider => provider.GetService<ConfigServiceConfiguration>().ProjectionStorage)
                    .AddScoped<ProjectionStoreContext>()
                    .AddScoped<IProjectionStore, ProjectionStore>()
                    .AddScoped<IStructureProjectionStore, StructureProjectionStore>()
                    .AddScoped<IEnvironmentProjectionStore, EnvironmentProjectionStore>()
                    .AddScoped<IConfigurationProjectionStore, ConfigurationProjectionStore>()
                    .AddScoped<IConfigurationCompiler, ConfigurationCompiler>()
                    .AddScoped<IJsonTranslator, JsonTranslator>()
                    .AddScoped<IConfigurationParser, ConfigurationParser>()
                    .AddSingleton<IEventStore, Services.EventStore>()
                    .AddSingleton<ESLogger, EventStoreLogger>()
                    .AddSingleton<IJsonTranslator, JsonTranslator>()
                    .AddSingleton(typeof(IDomainEventSerializer<>), typeof(DomainEventSerializer<>));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var config = Configuration.Get<ConfigServiceConfiguration>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseAuthentication()
               .UseCors(policy => policy.AllowAnyHeader()
                                        .AllowAnyMethod()
                                        .AllowAnyOrigin())
               .UseMvc()
               .UseSwagger()
               .UseSwaggerUI(options =>
               {
                   options.SwaggerEndpoint("/swagger/v2/swagger.json", string.Empty);
                   options.DocExpansion(DocExpansion.None);
                   options.DisplayRequestDuration();
                   options.OAuthClientId(config.Authentication.SwaggerClientId);
                   options.OAuthAppName("ConfigService Swagger");
               });
        }
    }
}