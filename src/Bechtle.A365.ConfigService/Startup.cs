using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Bechtle.A365.ConfigService.Authentication.Certificates;
using Bechtle.A365.ConfigService.Authentication.Certificates.Events;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Utilities;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations;
using Bechtle.A365.ConfigService.Implementations.SnapshotTriggers;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Middleware;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.Core.EventBus;
using Bechtle.A365.Core.EventBus.Abstraction;
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
using CertificateValidator = Bechtle.A365.ConfigService.Implementations.CertificateValidator;
using ESLogger = EventStore.ClientAPI.ILogger;

namespace Bechtle.A365.ConfigService
{
    /// <summary>
    ///     Service-Startup behaviour
    /// </summary>
    public class Startup
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<Startup> _logger;

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
                app.Configure(a => a.UseDeveloperExceptionPage(), _ => _logger.LogInformation("adding Development Exception-Handler"));
            else
                app.Configure(a => a.UseHsts(), _ => _logger.LogInformation("adding HSTS"))
                   .Configure(a => a.UseHttpsRedirection(), _ => _logger.LogInformation("adding HTTPS-Redirect"));

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
            RegisterMvc(services);
            RegisterVersioning(services);
            RegisterSwagger(services);
            RegisterAuthentication(services);
            RegisterDiServices(services);
            RegisterHealthEndpoints(services);
        }

        private void RegisterAuthentication(IServiceCollection services)
        {
            if (!_environment.EnvironmentName.Equals("docker", StringComparison.OrdinalIgnoreCase))
            {
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
            }
        }

        private void RegisterDiServices(IServiceCollection services)
        {
            _logger.LogInformation("Registering App-Services");

            // setup services for DI
            services.AddMemoryCache(options =>
                    {
                        var sizeInMb = Configuration.GetSection("MemoryCache:Local:SizeLimitInMb").Get<long>();
                        var sizeLimit = sizeInMb * 1024 * 1024;
                        var compactionPercentage = Configuration.GetSection("MemoryCache:Local:CompactionPercentage").Get<double>();

                        _logger.LogInformation($"configuring IMemoryCache, SizeLimit='{sizeLimit}', CompactionPercentage='{compactionPercentage}'");

                        options.SizeLimit = sizeLimit;
                        options.CompactionPercentage = compactionPercentage;
                    })
                    .AddStackExchangeRedisCache(options =>
                    {
                        var connectionString = Configuration.Get<ConfigServiceConfiguration>()?.MemoryCache?.Redis?.ConnectionString ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(connectionString))
                            throw new ArgumentNullException(nameof(connectionString), "MemoryCache:Redis:ConnectionString is null or empty");

                        options.Configuration = connectionString;
                    })
                    .AddScoped(_logger, provider => provider.GetService<IConfiguration>().Get<ConfigServiceConfiguration>())
                    .AddScoped(_logger, provider => provider.GetService<ConfigServiceConfiguration>().EventBusConnection)
                    .AddScoped(_logger, provider => provider.GetService<ConfigServiceConfiguration>().EventStoreConnection)
                    .AddScoped(_logger, provider => provider.GetService<ConfigServiceConfiguration>().Protected)
                    .AddScoped<IProjectionStore, ProjectionStore>(_logger)
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
                    .AddScoped<IStreamedStore, DomainObjectStore>(_logger)
                    .AddScoped<TimerSnapshotTrigger>(_logger)
                    .AddScoped<NumberThresholdSnapshotTrigger>(_logger)
                    .AddScoped<ISnapshotCreator, RoundtripSnapshotCreator>(_logger)
                    .AddSingleton<ICertificateValidator, CertificateValidator>(_logger)
                    .AddSingleton<IEventStore, Implementations.Stores.EventStore>(_logger)
                    .AddSingleton<ESLogger, EventStoreLogger>(_logger)
                    .AddSingleton<IJsonTranslator, JsonTranslator>(_logger)
                    .AddSingleton<IEventDeserializer, EventDeserializer>(_logger)
                    .AddSingleton(_logger, typeof(IDomainEventConverter<>), typeof(DomainEventConverter<>))
                    .AddHostedService<TemporaryKeyCleanupService>(_logger)
                    .AddHostedService<SnapshotService>(_logger)
                    .AddHostedService<IncrementalSnapshotService>(_logger);

            RegisterSnapshotStores(services);
        }

        private void RegisterHealthEndpoints(IServiceCollection services)
        {
            _logger.LogInformation("Registering Health Endpoint");
            _logger.LogDebug("building intermediate-service-provider");

            services.AddHealth(builder =>
            {
                builder.ServiceName = "ConfigService";
                builder.AnalyseInternalServices = true;
                builder.YellowStatuswWhenCheck("EventStore", () =>
                {
                    try
                    {
                        return Implementations.Stores.EventStore.ConnectionState switch
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

        private void RegisterMvc(IServiceCollection services)
        {
            _logger.LogInformation("registering MVC-Middleware with metrics-support");

            // setup MVC
            services.AddMvc()
                    .AddMetrics()
                    .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
        }

        private void RegisterSnapshotStores(IServiceCollection services)
        {
            var postgresSection = Configuration.GetSection("SnapshotConfiguration:Stores:Postgres");
            if (postgresSection.GetSection("Enabled").Get<bool>())
                services.AddScoped<ISnapshotStore, PostgresSnapshotStore>(_logger)
                        .AddDbContext<PostgresSnapshotStore.PostgresSnapshotContext>(
                            _logger,
                            (provider, builder) => { builder.UseNpgsql(postgresSection.GetSection("ConnectionString").Get<string>()); });

            var mssqlSection = Configuration.GetSection("SnapshotConfiguration:Stores:MsSql");
            if (mssqlSection.GetSection("Enabled").Get<bool>())
                services.AddScoped<ISnapshotStore, MsSqlSnapshotStore>(_logger)
                        .AddDbContext<MsSqlSnapshotStore.MsSqlSnapshotContext>(
                            _logger,
                            (provider, builder) => { builder.UseSqlServer(mssqlSection.GetSection("ConnectionString").Get<string>()); });

            var arangoSection = Configuration.GetSection("SnapshotConfiguration:Stores:Arango");
            if (arangoSection.GetSection("Enabled").Get<bool>())
                services.AddScoped<ISnapshotStore, ArangoSnapshotStore>().AddHttpClient<ArangoHttpClient>((provider, client) =>
                {
                    var config = provider.GetRequiredService<IConfiguration>().GetSection("SnapshotConfiguration:Stores:Arango");

                    var rawUri = config.GetSection("Uri").Get<string>();
                    if (!Uri.TryCreate(rawUri, UriKind.Absolute, out var arangoUri))
                    {
                        _logger.LogWarning($"unable to create URI from SnapshotConfiguration:Stores:Arango='{rawUri}'");
                        return;
                    }

                    client.BaseAddress = arangoUri;
                });
        }

        private void RegisterSwagger(IServiceCollection services)
        {
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
        }

        private void RegisterVersioning(IServiceCollection services)
        {
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
        }
    }
}