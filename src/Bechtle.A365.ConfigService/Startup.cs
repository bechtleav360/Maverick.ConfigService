using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Bechtle.A365.ConfigService.Authentication.Certificates;
using Bechtle.A365.ConfigService.Authentication.Certificates.Events;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Utilities;
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
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NLog.Web;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using CertificateValidator = Bechtle.A365.ConfigService.Services.CertificateValidator;
using ESLogger = EventStore.ClientAPI.ILogger;

namespace Bechtle.A365.ConfigService
{
    /// <summary>
    ///     Service-Startup behaviour
    /// </summary>
    public class Startup
    {
        private readonly ILogger<Startup> _logger;

        /// <inheritdoc />
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        public Startup(IConfiguration configuration,
                       ILogger<Startup> logger)
        {
            _logger = logger;
            Configuration = configuration;

            _logger.LogInformation(DebugUtilities.FormatConfiguration<ConfigServiceConfiguration>(configuration));
        }

        /// <summary>
        ///     Application-Configuration as defined in <see cref="Program" />
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        ///     Configures Application-Pipeline
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="provider"></param>
        public void Configure(IApplicationBuilder app,
                              IHostingEnvironment env,
                              IApiVersionDescriptionProvider provider)
        {
            _logger.LogInformation("adding authentication-hooks");

            app.UseAuthentication();

            if (env.IsDevelopment())
            {
                _logger.LogInformation("adding Development Exception-Handler");
                app.UseDeveloperExceptionPage();
            }
            else
            {
                _logger.LogInformation("adding HSTS");
                app.UseHsts();

                _logger.LogInformation("adding HTTPS-Redirect");
                app.UseHttpsRedirection();
            }

            _logger.LogInformation("finishing NLog configuration");

            app.ApplicationServices.SetupNLogServiceLocator();

            _logger.LogInformation("adding Correlation-Logging-Middleware");

            app.UseMiddleware<LoggingMiddleware>();

            _logger.LogInformation("adding CORS");

            app.UseCors(policy => policy.AllowAnyHeader()
                                        .AllowAnyMethod()
                                        .AllowAnyOrigin());

            _logger.LogInformation("adding Swagger/-UI");

            app.UseSwagger()
               .UseSwaggerUI(options =>
               {
                   options.DocExpansion(DocExpansion.None);
                   options.DisplayRequestDuration();
                   options.EnableDeepLinking();
                   options.EnableFilter();
                   options.ShowExtensions();

                   foreach (var description in provider.ApiVersionDescriptions.OrderByDescending(v => v.ApiVersion))
                       options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                                               $"ConfigService {description.GroupName.ToUpperInvariant()}");
               });

            _logger.LogInformation("adding MVC-Middleware");

            app.UseMvc();

            _logger.LogInformation("registering config-reload hook");

            ChangeToken.OnChange(Configuration.GetReloadToken,
                                 conf =>
                                 {
                                     Program.ConfigureNLog(conf, _logger);
                                     _logger.LogInformation(DebugUtilities.FormatConfiguration<ConfigServiceConfiguration>(conf));
                                 },
                                 Configuration);
        }

        /// <summary>
        ///     Configure DI-Services
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            _logger.LogInformation("registering MVC-Middleware");

            // setup MVC
            services.AddMvc()
                    .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            _logger.LogInformation("registering API-Version support");

            // setup API-Versioning and Swagger
            services.AddApiVersioning(options =>
                    {
                        options.AssumeDefaultVersionWhenUnspecified = true;
                        options.DefaultApiVersion = new ApiVersion(0, 0);
                        options.ReportApiVersions = true;
                    })
                    .AddVersionedApiExplorer(options =>
                    {
                        options.AssumeDefaultVersionWhenUnspecified = true;
                        options.DefaultApiVersion = new ApiVersion(0, 0);
                        options.GroupNameFormat = "'v'VVV";
                        options.SubstituteApiVersionInUrl = true;
                    });

            _logger.LogInformation("registering Swagger");

            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>()
                    .AddSwaggerGen(options =>
                    {
                        options.CustomSchemaIds(t => t.FullName);
                        options.OperationFilter<SwaggerDefaultValues>();
                        options.DescribeAllEnumsAsStrings();

                        var ass = Assembly.GetEntryAssembly();
                        if (!(ass is null))
                            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{ass.GetName().Name}.xml"));
                    });

            _logger.LogInformation("Registering Authentication-Services");

            // Cert-Based Authentication - if enabled
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

            _logger.LogInformation("Registering App-Services");

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

            _logger.LogInformation("Registering Health Endpoint");
            _logger.LogDebug("building intermediate-service-provider");

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

    /// <summary>
    ///     Configures the Swagger generation options.
    /// </summary>
    /// <remarks>
    ///     This allows API versioning to define a Swagger document per API version after the
    ///     <see cref="IApiVersionDescriptionProvider" /> service has been resolved from the service container.
    /// </remarks>
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConfigureSwaggerOptions" /> class.
        /// </summary>
        /// <param name="provider">The <see cref="IApiVersionDescriptionProvider">provider</see> used to generate Swagger documents.</param>
        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
        {
            _provider = provider;
        }

        /// <inheritdoc />
        public void Configure(SwaggerGenOptions options)
        {
            // add a swagger document for each discovered API version
            // note: you might choose to skip or document deprecated API versions differently
            foreach (var description in _provider.ApiVersionDescriptions)
                options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
        }

        private static Info CreateInfoForApiVersion(ApiVersionDescription description)
        {
            var info = new Info
            {
                Title = "Bechtle.A365.ConfigService",
                Version = description.ApiVersion.ToString(),
                Description = "Central Store for Application-Configurations in Maverick"
            };

            if (description.IsDeprecated)
                info.Description += " This API version has been deprecated.";

            return info;
        }
    }

    /// <summary>
    ///     Represents the Swagger/Swashbuckle operation filter used to document the implicit API version parameter.
    /// </summary>
    /// <remarks>
    ///     This <see cref="IOperationFilter" /> is only required due to bugs in the <see cref="SwaggerGenerator" />.
    ///     Once they are fixed and published, this class can be removed.
    /// </remarks>
    public class SwaggerDefaultValues : IOperationFilter
    {
        /// <summary>
        ///     Applies the filter to the specified operation using the given context.
        /// </summary>
        /// <param name="operation">The operation to apply the filter to.</param>
        /// <param name="context">The current operation filter context.</param>
        public void Apply(Operation operation, OperationFilterContext context)
        {
            var apiDescription = context.ApiDescription;

            operation.Deprecated = apiDescription.ActionDescriptor
                                                 .GetApiVersionModel(ApiVersionMapping.Explicit)
                                                 .DeprecatedApiVersions
                                                 .Contains(apiDescription.GetApiVersion());

            if (operation.Parameters == null)
                return;

            foreach (var parameter in operation.Parameters.OfType<NonBodyParameter>())
            {
                var description = apiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);

                if (parameter.Description == null)
                    parameter.Description = description.ModelMetadata?.Description;

                if (parameter.Default == null)
                    parameter.Default = description.DefaultValue;

                parameter.Required |= description.IsRequired;
            }
        }
    }
}