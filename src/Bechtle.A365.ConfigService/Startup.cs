using System;
using System.IO;
using System.Reflection;
using Bechtle.A365.ConfigService.Authentication.Certificates;
using Bechtle.A365.ConfigService.Authentication.Certificates.Events;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Middleware;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Services;
using Bechtle.A365.Maverick.Core.Health.Builder;
using Bechtle.A365.Maverick.Core.Health.Extensions;
using Bechtle.A365.Maverick.Core.Health.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NLog.Web;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;
using CertificateValidator = Bechtle.A365.ConfigService.Services.CertificateValidator;
using ESLogger = EventStore.ClientAPI.ILogger;

namespace Bechtle.A365.ConfigService
{
    /// <summary>
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseAuthentication();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.ApplicationServices.SetupNLogServiceLocator();

            app.UseMiddleware<LoggingMiddleware>()
               .UseCors(policy => policy.AllowAnyHeader()
                                        .AllowAnyMethod()
                                        .AllowAnyOrigin())
               .UseSwagger()
               .UseSwaggerUI(options =>
               {
                   options.SwaggerEndpoint("/swagger/v2/swagger.json", string.Empty);
                   options.DocExpansion(DocExpansion.None);
                   options.DisplayRequestDuration();
               })
               .UseMvc();
        }

        /// <summary>
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            // setup MVC
            services.AddMvc()
                    .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
                    .AddCertificate(options =>
                    {
                        options.Events = new CertificateAuthenticationEvents
                        {
                            OnAuthenticationFailed = context => context.HttpContext
                                                                       .RequestServices
                                                                       .GetService<ICertificateValidator>()
                                                                       .Fail(context),
                            OnValidateCertificate = context => context.HttpContext
                                                                      .RequestServices
                                                                      .GetService<ICertificateValidator>()
                                                                      .Validate(context)
                        };
                    });

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
            });

            // setup services for DI
            services.AddMemoryCache()
                    .AddSingleton<ICertificateValidator, CertificateValidator>()
                    .AddScoped(provider => provider.GetService<IConfiguration>().Get<ConfigServiceConfiguration>())
                    .AddScoped(provider => provider.GetService<ConfigServiceConfiguration>().EventBusConnection)
                    .AddScoped(provider => provider.GetService<ConfigServiceConfiguration>().EventStoreConnection)
                    .AddScoped(provider => provider.GetService<ConfigServiceConfiguration>().ProjectionStorage)
                    .AddScoped(provider => provider.GetService<ConfigServiceConfiguration>().Protected)
                    .AddDbContext<ProjectionStoreContext>(
                        (provider, builder) => builder.UseLoggerFactory(new NullLoggerFactory())
                                                      .UseSqlServer(provider.GetService<ProjectionStorageConfiguration>()
                                                                            .ConnectionString))
                    .AddScoped<IProjectionStore, ProjectionStore>()
                    .AddScoped<IStructureProjectionStore, StructureProjectionStore>()
                    .AddScoped<IEnvironmentProjectionStore, EnvironmentProjectionStore>()
                    .AddScoped<IConfigurationProjectionStore, ConfigurationProjectionStore>()
                    .AddScoped<IConfigurationCompiler, ConfigurationCompiler>()
                    .AddScoped<IJsonTranslator, JsonTranslator>()
                    .AddScoped<IConfigurationParser, AntlrConfigurationParser>()
                    .AddScoped<IConfigProtector, ConfigProtector>()
                    .AddScoped<IRegionEncryptionCertProvider, RegionEncryptionCertProvider>()
                    .AddScoped<IEventStore, Services.EventStore>()
                    .AddScoped<IDataExporter, DataExporter>()
                    .AddScoped<IDataImporter, DataImporter>()
                    .AddSingleton<ESLogger, EventStoreLogger>()
                    .AddSingleton<IJsonTranslator, JsonTranslator>()
                    .AddSingleton<IEventDeserializer, EventDeserializer>()
                    .AddSingleton(typeof(IDomainEventConverter<>), typeof(DomainEventConverter<>));

            // build the provider to grab the EventStore instance in the Health-Checks
            // @TODO: inject ServiceProvider into health-checks somehow
            var intermediateProvider = services.BuildServiceProvider();

            services.AddHealth(builder =>
            {
                var config = Configuration.Get<ConfigServiceConfiguration>();

                builder.ServiceName = "ConfigService";
                builder.AnalyseInternalServices = true;
                builder.RedStatusWhenDatabaseChecks(DatabaseType.MSSQL, config.ProjectionStorage.ConnectionString);
                builder.YellowStatuswWhenCheck("EventStore", () =>
                {
                    try
                    {
                        var store = intermediateProvider.GetService<IEventStore>();

                        if (store is null)
                            return new ServiceStatus("EventStore.Connection", ServiceState.Red);

                        switch (store.ConnectionState)
                        {
                            case ConnectionState.Connected:
                                return new ServiceStatus("EventStore.Connection", ServiceState.Green);

                            case ConnectionState.Reconnecting:
                                return new ServiceStatus("EventStore.Connection", ServiceState.Yellow)
                                {
                                    ErrorMessage = "connection to EventStore is unavailable, but it is being re-established"
                                };

                            // let's be exact what happens in what state...
                            // ReSharper disable once RedundantCaseLabel
                            case ConnectionState.Disconnected:
                            default:
                                return new ServiceStatus("EventStore.Connection", ServiceState.Red)
                                {
                                    ErrorMessage = "connection to EventStore is unavailable and NOT being re-established"
                                };
                        }
                    }
                    catch (Exception e)
                    {
                        return new ServiceStatus("EventStore.Connection", ServiceState.Red)
                        {
                            ErrorMessage = $"couldn't retrieve instance of {nameof(IEventStore)}; {e}"
                        };
                    }
                });
            });
        }
    }
}