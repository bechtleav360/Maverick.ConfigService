using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Bechtle.A365.ConfigService.Authentication.Certificates;
using Bechtle.A365.ConfigService.Authentication.Certificates.Events;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DbContexts;
using Bechtle.A365.ConfigService.Common.Utilities;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Implementations;
using Bechtle.A365.ConfigService.Implementations.Health;
using Bechtle.A365.ConfigService.Implementations.SnapshotTriggers;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Middleware;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.Core.EventBus;
using Bechtle.A365.Core.EventBus.Abstraction;
using Bechtle.A365.Maverick.Core.Health.Extensions;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NLog.Web;
using Prometheus;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using CertificateValidator = Bechtle.A365.ConfigService.Implementations.CertificateValidator;

namespace Bechtle.A365.ConfigService
{
    /// <summary>
    ///     Service-Startup behaviour
    /// </summary>
    public class Startup
    {
        private const string Liveness = "Liveness";
        private const string Readiness = "Readiness";
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
                  .Tweak(a => a.UseHttpMetrics(), "adding http-metrics")
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
                  .Tweak(a => a.UseEndpoints(builder => builder.MapControllers()), "adding controller-endpoints")
                  .Tweak(a => a.UseEndpoints(builder => builder.MapMetrics()), "adding metrics-endpoints")
                  .Tweak(a => a.UseEndpoints(builder => builder.MapHealthChecks("/health/ready", new HealthCheckOptions
                  {
                      Predicate = check => check.Tags.Contains(Readiness),
                      ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                  })), "adding /health/ready endpoint")
                  .Tweak(a => a.UseEndpoints(builder => builder.MapHealthChecks("/health/live", new HealthCheckOptions
                  {
                      Predicate = check => check.Tags.Contains(Liveness),
                      ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                  })), "adding /health/live endpoint")
                  .Tweak(a => a.UseEndpoints(builder => builder.MapHealthChecksUI()), "adding /healthchecks-ui endpoint")
                  .Tweak(_ =>
                  {
                      ChangeToken.OnChange(Configuration.GetReloadToken,
                                           conf =>
                                           {
                                               conf.ConfigureNLog(_logger);
                                               _logger.LogInformation(DebugUtilities.FormatConfiguration(conf));
                                           },
                                           Configuration);
                  }, "registering config-reload hook")
                  .Tweak(a => a.ApplicationServices.GetRequiredService<HttpPipelineCheck>().SetReady(), "settings http-pipeline to ready");

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
            RegisterSecretStores(services);
            RegisterHealthEndpoints(services);
        }

        private void ConfigureDbContext<TBuilder, TExtension>(RelationalDbContextOptionsBuilder<TBuilder, TExtension> options)
            where TBuilder : RelationalDbContextOptionsBuilder<TBuilder, TExtension>
            where TExtension : RelationalOptionsExtension, new()
        {
            options.MigrationsAssembly("Bechtle.A365.ConfigService.Migrations");
            options.MigrationsHistoryTable("__EFMigrationsHistory", SnapshotContext.Schema);
        }

        private void RegisterArangoSnapshotStore(IConfigurationSection section, IServiceCollection services)
            => services.AddScoped<ISnapshotStore, ArangoSnapshotStore>(_logger)
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
                               new AuthenticationHeaderValue(
                                   "Basic",
                                   Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{password}")));
                       });

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

        private void RegisterAzureSecretStore(IConfigurationSection section, IServiceCollection services)
            => services.AddScoped<ISecretConfigValueProvider, AzureSecretStore>(_logger);

        private void RegisterConfiguredSecretStore(IConfigurationSection section, IServiceCollection services)
            => services.AddScoped<ISecretConfigValueProvider, ConfiguredSecretStore>(_logger);

        private void RegisterDbContextSnapshotStore<TStore, TContext>(IServiceCollection services,
                                                                      Action<IServiceProvider, DbContextOptionsBuilder> optionsAction)
            where TStore : class, ISnapshotStore
            where TContext : DbContext
        {
            services.AddScoped<ISnapshotStore, TStore>(_logger)
                    .AddDbContext<TContext>(_logger, optionsAction);
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
                    .AddTransient<ILayerProjectionStore, LayerProjectionStore>(_logger)
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

                        if (string.IsNullOrWhiteSpace(config.Server))
                            throw new ArgumentException("EventBusConfiguration.Server is null or empty");

                        if (string.IsNullOrWhiteSpace(config.Hub))
                            throw new ArgumentException("EventBusConfiguration.Hub is null or empty");

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

            var signalrServer = Configuration.GetSection("EventBusConnection:Server").Get<string>();
            var signalrHub = Configuration.GetSection("EventBusConnection:Hub").Get<string>();

            services.AddSingleton<HttpPipelineCheck>();
            services.AddSingleton<EventStoreClusterCheck>();
            services.AddSingleton<EventStoreConnectionCheck>();
            services.AddHealthChecks()
                    .AddCheck<EventStoreClusterCheck>(
                        "EventStore-ConnectionType",
                        HealthStatus.Unhealthy,
                        new[] {Liveness},
                        TimeSpan.FromSeconds(2))
                    .AddCheck<HttpPipelineCheck>(
                        "Http-Pipeline",
                        HealthStatus.Unhealthy,
                        new[] {Liveness},
                        TimeSpan.FromSeconds(1))
                    .AddCheck<EventStoreConnectionCheck>(
                        "EventStore-Connection",
                        HealthStatus.Unhealthy,
                        new[] {Readiness},
                        TimeSpan.FromSeconds(30))
                    .AddRedis(Configuration.GetSection("MemoryCache:Redis:ConnectionString").Get<string>(),
                              "Temporary-Keys (Redis)",
                              HealthStatus.Unhealthy,
                              new[] {Readiness})
                    .AddSignalRHub(new Uri(new Uri(signalrServer, UriKind.Absolute),
                                           new Uri(signalrHub, UriKind.Relative))
                                       .ToString(),
                                   "SignalR-Connection",
                                   HealthStatus.Unhealthy,
                                   new[] {Readiness});

            services.AddHealth(builder =>
            {
                builder.ServiceName = "ConfigService";
                builder.AnalyseInternalServices = true;
            });

            services.AddHealthChecksUI(setup =>
            {
                setup.DisableDatabaseMigrations();
                setup.AddHealthCheckEndpoint("Ready Checks", "/health/ready");
                setup.AddHealthCheckEndpoint("Live Checks", "/health/live");
            }).AddInMemoryStorage();
        }

        private void RegisterLocalSnapshotStore(IConfigurationSection section, IServiceCollection services)
            => RegisterDbContextSnapshotStore<LocalFileSnapshotStore, SqliteSnapshotContext>(
                services,
                (provider, builder) => builder.UseSqlite(section.GetSection("ConnectionString").Get<string>(), ConfigureDbContext));

        private void RegisterMsSqlSnapshotStore(IConfigurationSection section, IServiceCollection services)
            => RegisterDbContextSnapshotStore<MsSqlSnapshotStore, MsSqlSnapshotContext>(
                services,
                (provider, builder) => builder.UseSqlServer(section.GetSection("ConnectionString").Get<string>(), ConfigureDbContext));

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
                    .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
        }

        private void RegisterOptions(IServiceCollection services)
        {
            // misc stuff
            services.Configure<KestrelAuthenticationConfiguration>(Configuration.GetSection("Authentication:Kestrel"));
            services.Configure<EventBusConnectionConfiguration>(Configuration.GetSection("EventBusConnection"));
            services.Configure<ProtectedConfiguration>(Configuration.GetSection("Protection"));
            services.Configure<EventStoreConnectionConfiguration>(Configuration.GetSection("EventStoreConnection"));

            // secret-stores
            services.Configure<ConfiguredSecretStoreConfiguration>(Configuration.GetSection("SecretConfiguration:Stores:Configuration"));
            services.Configure<AzureSecretStoreConfiguration>(Configuration.GetSection("SecretConfiguration:Stores:Azure"));
        }

        private void RegisterPostgresSnapshotStore(IConfigurationSection section, IServiceCollection services)
            => RegisterDbContextSnapshotStore<PostgresSnapshotStore, PostgresSnapshotContext>(
                services,
                (provider, builder) => builder.UseNpgsql(section.GetSection("ConnectionString").Get<string>(), ConfigureDbContext));

        private void RegisterSecretStores(IServiceCollection services)
        {
            var storeBaseSection = "SecretConfiguration:Stores";

            // define section => func that will be evaluated in order
            var storeRegistrations = new (string Section, Action<IConfigurationSection, IServiceCollection> RegistryFunc)[]
            {
                ("Configuration", RegisterConfiguredSecretStore),
                ("Azure", RegisterAzureSecretStore)
            };

            _logger.LogInformation($"looking for an enabled SecretStore ({storeBaseSection}:{{Store}}:[Enabled]) " +
                                   $"in this order ({string.Join(", ", storeRegistrations.Select(t => $"{t.Section}"))})");

            // look for all enabled stores, and collect some metadata
            var selectedStores = storeRegistrations.Select(t =>
                                                   {
                                                       var (section, registryFunc) = t;

                                                       var storeSection = Configuration.GetSection($"{storeBaseSection}:{section}");
                                                       return (SectionName: section,
                                                                  RegistryFunc: registryFunc,
                                                                  Section: storeSection,
                                                                  Enabled: storeSection.GetSection("Enabled")
                                                                                       .Get<bool>());
                                                   })
                                                   .Where(t => t.Enabled)
                                                   .ToList();

            if (selectedStores.Count == 0)
            {
                _logger.LogWarning($"no actual secret-stores have been registered, using {nameof(MemorySnapshotStore)} as fallback");
                services.AddScoped<ISecretConfigValueProvider, VoidSecretStore>(_logger);
                return;
            }

            if (selectedStores.Count > 1)
                _logger.LogError("multiple stores have been enabled (" +
                                 string.Join(", ", selectedStores.Select(t => t.SectionName)) +
                                 $"), but only one will be registered ({selectedStores.First().SectionName})");

            selectedStores[0].RegistryFunc?.Invoke(selectedStores[0].Section, services);
        }

        private void RegisterSnapshotStores(IServiceCollection services)
        {
            var storeBaseSection = "SnapshotConfiguration:Stores";

            // define section => func that will be evaluated in order
            var storeRegistrations = new (string Section, Action<IConfigurationSection, IServiceCollection> RegistryFunc)[]
            {
                ("Arango", RegisterArangoSnapshotStore),
                ("Local", RegisterLocalSnapshotStore),
                ("MsSql", RegisterMsSqlSnapshotStore),
                /*("Oracle", RegisterOracleSnapshotStore,)*/
                ("Postgres", RegisterPostgresSnapshotStore),
                ("Void", RegisterVoidSnapshotStore)
            };

            _logger.LogInformation($"looking for an enabled SnapshotStore ({storeBaseSection}:{{Store}}:[Enabled]) " +
                                   $"in this order ({string.Join(", ", storeRegistrations.Select(t => $"{t.Section}"))})");

            // look for all enabled stores, and collect some metadata
            var selectedStores = storeRegistrations.Select(t =>
                                                   {
                                                       var (section, registryFunc) = t;

                                                       var storeSection = Configuration.GetSection($"{storeBaseSection}:{section}");
                                                       return (SectionName: section,
                                                                  RegistryFunc: registryFunc,
                                                                  Section: storeSection,
                                                                  Enabled: storeSection.GetSection("Enabled")
                                                                                       .Get<bool>());
                                                   })
                                                   .Where(t => t.Enabled)
                                                   .ToList();

            if (selectedStores.Count == 0)
            {
                _logger.LogWarning($"no actual snapshot-stores have been registered, using {nameof(MemorySnapshotStore)} as fallback");
                services.AddScoped<ISnapshotStore, MemorySnapshotStore>(_logger);
                return;
            }

            if (selectedStores.Count > 1)
                _logger.LogError("multiple stores have been enabled (" +
                                 string.Join(", ", selectedStores.Select(t => t.SectionName)) +
                                 $"), but only one will be registered ({selectedStores.First().SectionName})");

            selectedStores[0].RegistryFunc?.Invoke(selectedStores[0].Section, services);
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

        private void RegisterVoidSnapshotStore(IConfigurationSection section, IServiceCollection services)
            => services.AddScoped<ISnapshotStore, VoidSnapshotStore>(_logger);
    }
}