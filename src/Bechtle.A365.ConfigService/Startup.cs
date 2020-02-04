using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Bechtle.A365.ConfigService.Authentication.Certificates;
using Bechtle.A365.ConfigService.Authentication.Certificates.Events;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.Utilities;
using Bechtle.A365.ConfigService.Configuration;
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

        /// <inheritdoc cref="Startup" />
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="env"></param>
        public Startup(IConfiguration configuration, ILogger<Startup> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _environment = env;
            Configuration = configuration;

            _logger.LogInformation(DebugUtilities.FormatConfiguration(configuration));
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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
            => app.StartTweakingWith(_logger, Configuration)
                  .Tweak(a => a.UseRouting(), "adding routing")
                  .TweakWhen(c => c.GetSection("EnableLegacyRedirect"),
                             a => a.UseMiddleware<V0RedirectMiddleware>(),
                             "adding V0-Redirect-Middleware")
                  .TweakWhen(c => c.GetSection("Authentication:Kestrel:Enabled"),
                             a => a.UseAuthentication(),
                             "adding authentication-hooks",
                             "skipping authentication-hooks")
                  .TweakWhen(env.IsDevelopment(), a => a.UseDeveloperExceptionPage(), "adding Development Exception-Handler")
                  .TweakWhen(!env.IsDevelopment(), a => a.UseHsts(), "adding HSTS")
                  .TweakWhen(!env.IsDevelopment(), a => a.UseHttpsRedirection(), "adding HTTPS-Redirect")
                  .Tweak(a => a.ApplicationServices.SetupNLogServiceLocator(), "finishing NLog configuration")
                  .Tweak(a => a.UseMiddleware<LoggingMiddleware>(), "adding Correlation-Logging-Middleware")
                  .Tweak(a => a.UseCors(policy => policy.AllowAnyHeader()
                                                        .AllowAnyMethod()
                                                        .AllowAnyOrigin()),
                         "adding CORS")
                  .Tweak(a => a.UseSwagger()
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
                         "adding Swagger/-UI")
                  .Tweak(a => a.UseEndpoints(routes => routes.MapControllerRoute("Health", "api/health/status/{depth?}", new
                         {
                             controller = "Health",
                             action = "Status"
                         })),
                         "adding Health-Middleware")
                  .Tweak(a => a.UseMetricsActiveRequestMiddleware(), "adding active-request metrics")
                  .Tweak(a => a.UseMetricsApdexTrackingMiddleware(), "adding apdex metrics")
                  .Tweak(a => a.UseMetricsErrorTrackingMiddleware(), "adding error metrics")
                  .Tweak(a => a.UseMetricsOAuth2TrackingMiddleware(), "adding oauth metrics")
                  .Tweak(a => a.UseMetricsPostAndPutSizeTrackingMiddleware(), "adding request-size metrics")
                  .Tweak(a => a.UseMetricsRequestTrackingMiddleware(), "adding request-path metrics")
                  .Tweak(a => a.UseMetricsTextEndpoint(), "adding text-metrics endpoint")
                  .Tweak(a => a.UseMetricsEndpoint(), "adding metrics endpoint")
                  .Tweak(a => a.UseEndpoints(builder => builder.MapControllers()), "adding controller-endpoints")
                  .Tweak(_ =>
                  {
                      ChangeToken.OnChange(Configuration.GetReloadToken,
                                           conf =>
                                           {
                                               conf.ConfigureNLog(_logger);
                                               _logger.LogInformation(DebugUtilities.FormatConfiguration(conf));
                                           },
                                           Configuration);
                  }, "registering config-reload hook");

        /// <summary>
        ///     Configure DI-Services
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            RegisterOptions(services);
            RegisterMvc(services);
            RegisterVersioning(services);
            RegisterSwagger(services);
            RegisterAuthentication(services);
            RegisterDiServices(services);
            RegisterSnapshotStores(services);
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
                        var connectionString = Configuration.GetSection("MemoryCache:Redis:ConnectionString").Get<string>();

                        if (string.IsNullOrWhiteSpace(connectionString))
                            throw new ArgumentException("configuration MemoryCache:Redis:ConnectionString is null or empty", nameof(options));

                        options.Configuration = connectionString;
                    })
                    .AddTransient<IProjectionStore, ProjectionStore>(_logger)
                    .AddTransient<IStructureProjectionStore, StructureProjectionStore>(_logger)
                    .AddTransient<IEnvironmentProjectionStore, EnvironmentProjectionStore>(_logger)
                    .AddTransient<IConfigurationProjectionStore, ConfigurationProjectionStore>(_logger)
                    .AddTransient<ITemporaryKeyStore, TemporaryKeyStore>(_logger)
                    .AddTransient<IConfigurationCompiler, ConfigurationCompiler>(_logger)
                    .AddTransient<IJsonTranslator, JsonTranslator>(_logger)
                    .AddTransient<IConfigurationParser, AntlrConfigurationParser>(_logger)
                    .AddTransient<IConfigProtector, ConfigProtector>(_logger)
                    .AddTransient<IRegionEncryptionCertProvider, RegionEncryptionCertProvider>(_logger)
                    .AddTransient<IDataExporter, DataExporter>(_logger)
                    .AddTransient<IDataImporter, DataImporter>(_logger)
                    .AddTransient<IEventBus, WebSocketEventBusClient>(_logger, provider =>
                    {
                        var config = provider.GetRequiredService<IOptionsMonitor<EventBusConnectionConfiguration>>().CurrentValue;

                        return new WebSocketEventBusClient(new Uri(new Uri(config.Server), config.Hub).ToString(),
                                                           provider.GetService<ILoggerFactory>());
                    })
                    .AddTransient<ICommandValidator, InternalDataCommandValidator>(_logger)
                    .AddTransient<IDomainObjectStore, DomainObjectStore>(_logger)
                    .AddTransient<TimerSnapshotTrigger>(_logger)
                    .AddTransient<NumberThresholdSnapshotTrigger>(_logger)
                    .AddTransient<OnDemandSnapshotTrigger>(_logger)
                    .AddTransient<ISnapshotCreator, RoundtripSnapshotCreator>(_logger)
                    .AddSingleton<ICertificateValidator, CertificateValidator>(_logger)
                    .AddSingleton<IEventStore, Implementations.Stores.EventStore>(_logger)
                    .AddSingleton<ESLogger, EventStoreLogger>(_logger)
                    .AddSingleton<IJsonTranslator, JsonTranslator>(_logger)
                    .AddSingleton<IEventDeserializer, EventDeserializer>(_logger)
                    .AddSingleton(_logger, typeof(IDomainEventConverter<>), typeof(DomainEventConverter<>))
                    .AddHostedService<GracefulShutdownService>(_logger)
                    .AddHostedService<TemporaryKeyCleanupService>(_logger)
                    .AddHostedService<SnapshotService>(_logger)
                    .AddHostedService<IncrementalSnapshotService>(_logger);
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
                    .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.AllowTrailingCommas = true;
                        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    })
                    .AddMetrics()
                    .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
        }

        private void RegisterOptions(IServiceCollection services)
        {
            services.Configure<KestrelAuthenticationConfiguration>(Configuration.GetSection("Authentication:Kestrel"));
            services.Configure<EventBusConnectionConfiguration>(Configuration.GetSection("EventBusConnection"));
            services.Configure<ProtectedConfiguration>(Configuration.GetSection("Protection"));
            services.Configure<EventStoreConnectionConfiguration>(Configuration.GetSection("EventStoreConnection"));
        }

        private void RegisterSnapshotStores(IServiceCollection services)
        {
            var storeRegistration = new[]
            {
                TryAddArangoSnapshotStore(services),
                TryAddLocalSnapshotStore(services),
                TryAddMsSqlSnapshotStore(services),
                TryAddPostgresSnapshotStore(services),
                TryAddVoidSnapshotStore(services)
            };

            // if no SnapshotStore was registered, register Memory-Store as fallback
            if (storeRegistration.Any(r => r))
                return;

            _logger.LogWarning($"no actual snapshot-stores have been registered, using {nameof(MemorySnapshotStore)} as fallback");
            services.AddScoped<ISnapshotStore, MemorySnapshotStore>(_logger);
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

        private bool TryAddArangoSnapshotStore(IServiceCollection services)
        {
            var arangoSection = Configuration.GetSection("SnapshotConfiguration:Stores:Arango");
            if (!arangoSection.GetSection("Enabled").Get<bool>())
                return false;

            services.AddScoped<ISnapshotStore, ArangoSnapshotStore>(_logger)
                    .AddHttpClient("Arango", (provider, client) =>
                    {
                        var config = provider.GetRequiredService<IConfiguration>().GetSection("SnapshotConfiguration:Stores:Arango");

                        var rawUri = config.GetSection("Uri").Get<string>();
                        if (!Uri.TryCreate(rawUri, UriKind.Absolute, out var arangoUri))
                        {
                            _logger.LogWarning($"unable to create URI from SnapshotConfiguration:Stores:Arango:Uri='{rawUri}'");
                            return;
                        }

                        client.BaseAddress = arangoUri;

                        var user = config.GetSection("User").Get<string>();
                        var password = config.GetSection("Password").Get<string>();

                        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
                        {
                            _logger.LogWarning("unable to locate User / Password (SnapshotConfiguration:Stores:Arango:[User|Password])");
                            return;
                        }

                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{password}")));
                    });

            return true;
        }

        private bool TryAddLocalSnapshotStore(IServiceCollection services)
        {
            var localSection = Configuration.GetSection("SnapshotConfiguration:Stores:Local");
            if (!localSection.GetSection("Enabled").Get<bool>())
                return false;

            services.AddScoped<ISnapshotStore, LocalFileSnapshotStore>(_logger)
                    .AddDbContext<LocalFileSnapshotStore.LocalFileSnapshotContext>(
                        _logger,
                        (provider, builder) => { builder.UseSqlite(localSection.GetSection("ConnectionString").Get<string>()); });

            return true;
        }

        private bool TryAddMsSqlSnapshotStore(IServiceCollection services)
        {
            var mssqlSection = Configuration.GetSection("SnapshotConfiguration:Stores:MsSql");
            if (!mssqlSection.GetSection("Enabled").Get<bool>())
                return false;

            services.AddScoped<ISnapshotStore, MsSqlSnapshotStore>(_logger)
                    .AddDbContext<MsSqlSnapshotStore.MsSqlSnapshotContext>(
                        _logger,
                        (provider, builder) => { builder.UseSqlServer(mssqlSection.GetSection("ConnectionString").Get<string>()); });

            return true;
        }

        private bool TryAddPostgresSnapshotStore(IServiceCollection services)
        {
            var postgresSection = Configuration.GetSection("SnapshotConfiguration:Stores:Postgres");
            if (!postgresSection.GetSection("Enabled").Get<bool>())
                return false;

            services.AddScoped<ISnapshotStore, PostgresSnapshotStore>(_logger)
                    .AddDbContext<PostgresSnapshotStore.PostgresSnapshotContext>(
                        _logger,
                        (provider, builder) => { builder.UseNpgsql(postgresSection.GetSection("ConnectionString").Get<string>()); });

            return true;
        }

        private bool TryAddVoidSnapshotStore(IServiceCollection services)
        {
            var voidSection = Configuration.GetSection("SnapshotConfiguration:Stores:Void");
            if (!voidSection.GetSection("Enabled").Get<bool>())
                return false;

            services.AddScoped<ISnapshotStore, VoidSnapshotStore>(_logger);

            return true;
        }
    }
}