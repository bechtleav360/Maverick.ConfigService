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
using Bechtle.A365.ConfigService.Services.Stores;
using Bechtle.A365.Core.EventBus;
using Bechtle.A365.Core.EventBus.Abstraction;
using Bechtle.A365.Maverick.Core.Health.Builder;
using Bechtle.A365.Maverick.Core.Health.Extensions;
using Bechtle.A365.Maverick.Core.Health.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NLog.Web;
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
        private readonly IWebHostEnvironment _environment;

        /// <inheritdoc />
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="env"></param>
        public Startup(IConfiguration configuration, ILogger<Startup> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _environment = env;
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
                              IWebHostEnvironment env,
                              IApiVersionDescriptionProvider provider)
        {
            app.Configure(a => a.UseRouting(), _ => _logger.LogInformation("adding routing"));

            if (Configuration.GetSection("Authentication:Kestrel:Enabled").Get<bool>())
                app.Configure(a => a.UseAuthentication(), _ => _logger.LogInformation("adding authentication-hooks"));
            else
                _logger.LogInformation("skipping authentication-hooks");

            if (env.IsDevelopment())
            {
                app.Configure(a => a.UseDeveloperExceptionPage(), _ => _logger.LogInformation("adding Development Exception-Handler"));
            }
            else
            {
                app.Configure(a => a.UseHsts(), _ => _logger.LogInformation("adding HSTS"))
                   .Configure(a => a.UseHttpsRedirection(), _ => _logger.LogInformation("adding HTTPS-Redirect"));
            }

            app.Configure(a => a.ApplicationServices.SetupNLogServiceLocator(), _ => _logger.LogInformation("finishing NLog configuration"))
               .Configure(a => a.UseMiddleware<LoggingMiddleware>(), _ => _logger.LogInformation("adding Correlation-Logging-Middleware"))
               .Configure(a => a.UseCors(policy => policy.AllowAnyHeader()
                                                         .AllowAnyMethod()
                                                         .AllowAnyOrigin()),
                          _ => _logger.LogInformation("adding CORS"))
               .Configure(a => a.UseSwagger()
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
                                }),
                          _ => _logger.LogInformation("adding Swagger/-UI"))
               .Configure(a => a.UseEndpoints(routes => routes.MapControllerRoute("Health", "api/health/status/{depth?}", new
                          {
                              controller = "Health",
                              action = "Status"
                          })),
                          _ => _logger.LogInformation("adding Health-Middleware"))
               .Configure(a => a.UseMetricsActiveRequestMiddleware(), _ => _logger.LogInformation("adding active-request metrics"))
               .Configure(a => a.UseMetricsApdexTrackingMiddleware(), _ => _logger.LogInformation("adding apdex metrics"))
               .Configure(a => a.UseMetricsErrorTrackingMiddleware(), _ => _logger.LogInformation("adding error metrics"))
               .Configure(a => a.UseMetricsOAuth2TrackingMiddleware(), _ => _logger.LogInformation("adding oauth metrics"))
               .Configure(a => a.UseMetricsPostAndPutSizeTrackingMiddleware(), _ => _logger.LogInformation("adding request-size metrics"))
               .Configure(a => a.UseMetricsRequestTrackingMiddleware(), _ => _logger.LogInformation("adding request-path metrics"))
               .Configure(a => a.UseMetricsTextEndpoint(), _ => _logger.LogInformation("adding text-metrics endpoint"))
               .Configure(a => a.UseMetricsEndpoint(), _ => _logger.LogInformation("adding metrics endpoint"))
               .Configure(a => a.UseEndpoints(builder => builder.MapControllers()), _ => _logger.LogInformation("adding controller-endpoints"));

            _logger.LogInformation("registering config-reload hook");

            ChangeToken.OnChange(Configuration.GetReloadToken,
                                 conf =>
                                 {
                                     conf.ConfigureNLog(_logger);
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
            _logger.LogInformation("registering MVC-Middleware with metrics-support");

            // setup MVC
            services.AddMvc()
                    .AddMetrics()
                    .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

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

                        var ass = Assembly.GetEntryAssembly();
                        if (!(ass is null))
                            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{ass.GetName().Name}.xml"));
                    });

            _logger.LogInformation("Registering Authentication-Services");

            if (!_environment.EnvironmentName.Equals("docker", StringComparison.OrdinalIgnoreCase))
            {
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
            }

            _logger.LogInformation("Registering App-Services");

            // setup services for DI
            services.AddMemoryCache()
                    .AddStackExchangeRedisCache(options =>
                    {
                        var connectionString = Configuration.Get<ConfigServiceConfiguration>()?.MemoryCache?.Redis?.ConnectionString ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(connectionString))
                            throw new ArgumentNullException(nameof(connectionString), "MemoryCache:Redis:ConnectionString is null or empty");

                        options.Configuration = connectionString;
                    })
                    .AddSingleton<ICertificateValidator, CertificateValidator>(_logger)
                    .AddScoped(_logger, provider => provider.GetService<IConfiguration>().Get<ConfigServiceConfiguration>())
                    .AddScoped(_logger, provider => provider.GetService<ConfigServiceConfiguration>().EventBusConnection)
                    .AddScoped(_logger, provider => provider.GetService<ConfigServiceConfiguration>().EventStoreConnection)
                    .AddScoped(_logger, provider => provider.GetService<ConfigServiceConfiguration>().ProjectionStorage)
                    .AddScoped(_logger, provider => provider.GetService<ConfigServiceConfiguration>().Protected)
                    .AddDbContext<ProjectionStoreContext>(
                        _logger,
                        (provider, builder) =>
                        {
                            var settings = provider.GetService<ProjectionStorageConfiguration>();

                            // @IMPORTANT: when handling additional cases here, don't forget to update the error-messages
                            switch (settings.Backend)
                            {
                                case DbBackend.MsSql:
                                    _logger.LogTrace("using MsSql database-backend");
                                    builder.UseSqlServer(settings.ConnectionString);
                                    break;

                                case DbBackend.Postgres:
                                    _logger.LogTrace("using PostgreSql database-backend");
                                    builder.UseNpgsql(settings.ConnectionString);
                                    break;

                                // let's be explicit what happens for UNSET or NON-IMPLEMENTED backends
                                // ReSharper disable once RedundantCaseLabel
                                case DbBackend.None:
                                default:
                                    _logger.LogError($"Unsupported DbBackend: '{settings.Backend}'; " +
                                                     $"change ProjectionStorage:Backend; " +
                                                     $"set either {DbBackend.MsSql:G} or {DbBackend.Postgres:G} as Db-Backend");
                                    throw new ArgumentOutOfRangeException(nameof(settings.Backend),
                                                                          $"Unsupported DbBackend: '{settings.Backend}'; " +
                                                                          $"change ProjectionStorage:Backend; " +
                                                                          $"set either {DbBackend.MsSql:G} or {DbBackend.Postgres:G} as Db-Backend");
                            }
                        })
                    .AddScoped<IProjectionStore, ProjectionStore>(_logger)
                    .AddScoped<IMetadataProjectionStore, MetadataProjectionStore>(_logger)
                    .AddScoped<IStructureProjectionStore, StructureProjectionStore>(_logger)
                    .AddScoped<IEnvironmentProjectionStore, EnvironmentProjectionStore>(_logger)
                    .AddScoped<IConfigurationProjectionStore, ConfigurationProjectionStore>(_logger)
                    .AddScoped<ITemporaryKeyStore, TemporaryKeyStore>(_logger)
                    .AddScoped<IConfigurationCompiler, ConfigurationCompiler>(_logger)
                    .AddScoped<IJsonTranslator, JsonTranslator>(_logger)
                    .AddScoped<IConfigurationParser, AntlrConfigurationParser>(_logger)
                    .AddScoped<IConfigProtector, ConfigProtector>(_logger)
                    .AddScoped<IRegionEncryptionCertProvider, RegionEncryptionCertProvider>(_logger)
                    .AddScoped<IDataExporter, DataExporter>(_logger)
                    .AddScoped<IDataImporter, DataImporter>(_logger)
                    .AddScoped<IEventBus, WebSocketEventBusClient>(_logger, provider =>
                    {
                        var config = provider.GetService<EventBusConnectionConfiguration>();

                        return new WebSocketEventBusClient(new Uri(new Uri(config.Server), config.Hub).ToString(),
                                                           provider.GetService<ILoggerFactory>());
                    })
                    .AddScoped<ICommandValidator, InternalDataCommandValidator>(_logger)
                    .AddScoped<IEventHistoryService, MemoryEventHistoryService>(_logger)
                    .AddSingleton<IEventStore, Services.Stores.EventStore>(_logger)
                    .AddSingleton<ESLogger, EventStoreLogger>(_logger)
                    .AddSingleton<IJsonTranslator, JsonTranslator>(_logger)
                    .AddSingleton<IEventDeserializer, EventDeserializer>(_logger)
                    .AddSingleton(_logger, typeof(IDomainEventConverter<>), typeof(DomainEventConverter<>))
                    .AddHostedService<TemporaryKeyCleanupService>(_logger);

            _logger.LogInformation("Registering Health Endpoint");
            _logger.LogDebug("building intermediate-service-provider");

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
                        return Services.Stores.EventStore.ConnectionState switch
                        {
                            ConnectionState.Connected => new ServiceStatus("EventStore.Connection", ServiceState.Green),
                            ConnectionState.Reconnecting => new ServiceStatus("EventStore.Connection", ServiceState.Yellow)
                            {
                                ErrorMessage = "connection to EventStore is unavailable, but it is being re-established"
                            },
                            ConnectionState.Disconnected => new ServiceStatus("EventStore.Connection", ServiceState.Red)
                            {
                                ErrorMessage = "connection to EventStore is unavailable and NOT being re-established"
                            },
                            _ => new ServiceStatus("EventStore.Connection", ServiceState.Red)
                            {
                                ErrorMessage = "connection to EventStore is unavailable and NOT being re-established"
                            }
                        };
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