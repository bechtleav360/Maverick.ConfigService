using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Utilities;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Implementations;
using Bechtle.A365.ConfigService.Implementations.Health;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Middleware;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.Core.EventBus;
using Bechtle.A365.Core.EventBus.Abstraction;
using Bechtle.A365.ServiceBase;
using Bechtle.A365.ServiceBase.EventStore.Extensions;
using Bechtle.A365.ServiceBase.Sagas.MessageBroker;
using HealthChecks.UI.Client;
using Maverick.Extensions.CorrelationIds;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NLog.Web;
using Prometheus;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using static Bechtle.A365.ConfigService.Common.Utilities.ApplicationBuilderExtensions;

namespace Bechtle.A365.ConfigService
{
    /// <summary>
    ///     Service-Startup behaviour
    /// </summary>
    public class Startup : DefaultStartup
    {
        private const string Liveness = "Liveness";
        private const string Readiness = "Readiness";
        private const string Status = "Status";
        private ILogger<Startup>? _logger;

        /// <inheritdoc cref="Startup" />
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration) : base(configuration)
        {
        }

        /// <inheritdoc />
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
        {
            _logger = app.ApplicationServices.GetRequiredService<ILogger<Startup>>();
            AppConfigContainer appConfiguration = app.StartTweakingWith(Configuration, _logger);

            appConfiguration.Tweak(
                a => a.UseCorrelationIds(
                    new CorrelationCoercionOptions
                    {
                        PossibleHeaders = new List<string>
                        {
                            "x-correlation-id",
                            "X-FOOBAR-ID"
                        }
                    }),
                "using legacy correlation-ids");

            // basic Asp.NetCore 3.X stuff
            appConfiguration.Tweak(a => a.UseRouting(), "adding routing")
                            .Tweak(a => a.UseHttpMetrics(), "adding http-metrics");

            // optional legacy-redirect for clients that don't support versioned endpoints yet
            appConfiguration.TweakWhen(
                c => c.GetSection("EnableLegacyRedirect"),
                a => a.UseMiddleware<V0RedirectMiddleware>(),
                "adding V0-Redirect-Middleware");

            // debugging-stuff
            appConfiguration.TweakWhen(env.IsDevelopment(), a => a.UseDeveloperExceptionPage(), "adding Development Exception-Handler");

            // Https-Upgrade for non-dev environments
            appConfiguration.TweakWhen(!env.IsDevelopment(), a => a.UseHsts(), "adding HSTS")
                            .TweakWhen(!env.IsDevelopment(), a => a.UseHttpsRedirection(), "adding HTTPS-Redirect");

            appConfiguration.Tweak(a => a.ApplicationServices.SetupNLogServiceLocator(), "finishing NLog configuration");
            appConfiguration.Tweak(a => a.UseMiddleware<LoggingMiddleware>(), "adding Correlation-Logging-Middleware");

            // cors
            appConfiguration.Tweak(
                a => a.UseCors(
                    policy => policy.AllowAnyHeader()
                                    .AllowAnyMethod()
                                    .AllowAnyOrigin()),
                "adding CORS");

            // OpenApi / Swagger
            appConfiguration.Tweak(
                a => a.UseSwagger()
                      .UseSwaggerUI(
                          options =>
                          {
                              options.DefaultModelExpandDepth(0);
                              options.DefaultModelRendering(ModelRendering.Example);
                              options.DocExpansion(DocExpansion.None);
                              options.DisplayRequestDuration();
                              options.EnableDeepLinking();
                              options.EnableFilter();
                              options.ShowExtensions();

                              foreach (ApiVersionDescription description in provider.ApiVersionDescriptions.OrderByDescending(v => v.ApiVersion))
                              {
                                  options.SwaggerEndpoint(
                                      $"/swagger/{description.GroupName}/swagger.json",
                                      $"ConfigService {description.GroupName.ToUpperInvariant()}");
                              }
                          }),
                "adding Swagger/-UI");

            // Add Controllers to Http-Pipeline
            // Don't do this before other Endpoints, so they get the chance to handle stuff before the Controllers do
            appConfiguration.Tweak(a => a.UseEndpoints(builder => builder.MapControllers()), "adding controller-endpoints");

            // Metrics and Live-/Readiness-Probes
            appConfiguration.Tweak(a => a.UseEndpoints(builder => builder.MapMetrics()), "adding metrics-endpoints");
            appConfiguration.Tweak(
                                a => a.UseEndpoints(
                                    builder => builder.MapHealthChecks(
                                        "/health/ready",
                                        new HealthCheckOptions
                                        {
                                            Predicate = check => check.Tags.Contains(Readiness),
                                            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                                        })),
                                "adding /health/ready endpoint")
                            .Tweak(
                                a => a.UseEndpoints(
                                    builder => builder.MapHealthChecks(
                                        "/health/live",
                                        new HealthCheckOptions
                                        {
                                            Predicate = check => check.Tags.Contains(Liveness),
                                            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                                        })),
                                "adding /health/live endpoint")
                            .Tweak(
                                a => a.UseEndpoints(
                                    builder => builder.MapHealthChecks(
                                        "/health/status",
                                        new HealthCheckOptions
                                        {
                                            Predicate = check => check.Tags.Contains(Status),
                                            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                                        })),
                                "adding /health/status endpoint");

            // re-load configuration when possible
            appConfiguration.Tweak(
                _ =>
                {
                    ChangeToken.OnChange(
                        Configuration.GetReloadToken,
                        conf =>
                        {
                            conf.ConfigureNLog(_logger);
                            _logger.LogInformation(DebugUtilities.FormatConfiguration(conf));
                        },
                        Configuration);
                },
                "registering config-reload hook");

            // mark end of startup-phase
            appConfiguration.Tweak(a => a.ApplicationServices.GetRequiredService<HttpPipelineCheck>().SetReady(), "settings http-pipeline to ready");
        }

        /// <inheritdoc />
        public override void ConfigureServices(IServiceCollection services)
        {
            IMvcBuilder mvcBuilder = services.AddControllers();

            ConfigureMvc(mvcBuilder);

            // Swagger
            services.TryAddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            services.AddSwaggerGen(
                options =>
                {
                    ConfigureSwaggerGen(options);
                    ConfigureAssemblyDocumentation(options);
                });

            // setup API-Versioning and Swagger
            services.AddApiVersioning(ConfigureApiVersioning)
                    .AddVersionedApiExplorer(ConfigureVersionedApiExplorer);

            // Options
            services.AddOptions();
            services.Configure<MessageBrokerOptions>(Configuration.GetSection("Sagas:MessageBroker"));

            // HttpClients
            services.AddHttpClient();

            // hook for custom Serializer-Settings
            RegisterJsonSettingsProvider(services);

            AddServiceConfiguration(services);
        }

        /// <inheritdoc />
        protected override void AddServiceConfiguration(IServiceCollection services)
        {
            RegisterOptions(services);
            RegisterMvc(services);
            RegisterVersioning(services);
            RegisterDiServices(services);
            RegisterSecretStores(services);
            RegisterHealthEndpoints(services);
        }

        /// <summary>
        ///     Configure if and which Xml-Docs get loaded from files
        /// </summary>
        /// <param name="options"></param>
        protected override void ConfigureAssemblyDocumentation(SwaggerGenOptions options)
        {
            foreach (Assembly? ass in LoadAssemblies().Concat(new[] { Assembly.GetEntryAssembly(), Assembly.GetExecutingAssembly() }))
            {
                if (ass is null)
                {
                    continue;
                }

                string docFile = Path.Combine(AppContext.BaseDirectory, $"{ass.GetName().Name}.xml");
                if (File.Exists(docFile))
                {
                    options.IncludeXmlComments(docFile);
                }
            }
        }

        private void RegisterAzureSecretStore(IConfigurationSection section, IServiceCollection services)
            => services.AddScoped<ISecretConfigValueProvider, AzureSecretStore>();

        private void RegisterConfiguredSecretStore(IConfigurationSection section, IServiceCollection services)
            => services.AddScoped<ISecretConfigValueProvider, ConfiguredSecretStore>();

        private void RegisterDiServices(IServiceCollection services)
        {
            // setup services for DI
            services.AddMemoryCache()
                    .AddStackExchangeRedisCache(
                        options =>
                        {
                            var connectionString = Configuration.GetSection("MemoryCache:Redis:ConnectionString").Get<string>();

                            if (string.IsNullOrWhiteSpace(connectionString))
                            {
                                throw new ArgumentException("configuration MemoryCache:Redis:ConnectionString is null or empty", nameof(options));
                            }

                            options.Configuration = connectionString;
                        })
                    .AddScoped<IProjectionStore, ProjectionStore>()
                    .AddScoped<ILayerProjectionStore, LayerProjectionStore>()
                    .AddScoped<IStructureProjectionStore, StructureProjectionStore>()
                    .AddScoped<IEnvironmentProjectionStore, EnvironmentProjectionStore>()
                    .AddScoped<IConfigurationProjectionStore, ConfigurationProjectionStore>()
                    .AddScoped<ITemporaryKeyStore, TemporaryKeyStore>()
                    .AddTransient<IConfigurationCompiler, ConfigurationCompiler>()
                    .AddScoped<IJsonTranslator, JsonTranslator>()
                    .AddScoped<IConfigurationParser, AntlrConfigurationParser>()
                    .AddScoped<IDataExporter, DataExporter>()
                    .AddScoped<IDataImporter, DataImporter>()
                    .AddTransient<IEventBus, WebSocketEventBusClient>(
                        provider =>
                        {
                            EventBusConnectionConfiguration config = provider.GetRequiredService<IOptionsMonitor<EventBusConnectionConfiguration>>()
                                                                             .CurrentValue;

                            if (string.IsNullOrWhiteSpace(config.Server))
                            {
                                throw new ArgumentException("EventBusConfiguration.Server is null or empty");
                            }

                            if (string.IsNullOrWhiteSpace(config.Hub))
                            {
                                throw new ArgumentException("EventBusConfiguration.Hub is null or empty");
                            }

                            return new WebSocketEventBusClient(
                                new Uri(new Uri(config.Server), config.Hub).ToString(),
                                provider.GetService<ILoggerFactory>());
                        })
                    .AddTransient<ICommandValidator, InternalDataCommandValidator>()
                    .AddTransient<IDomainObjectStoreLocationProvider, DomainObjectStoreLocationProvider>()
                    .AddTransient<IDomainObjectFileStore, DomainObjectFileStore>()
                    .AddTransient<IDomainObjectStore, DomainObjectStore>()
                    .AddTransient<IDomainObjectManager, DomainObjectManager>()
                    .AddTransient<IEventStoreOptionsProvider, EventStoreOptionsProvider>()
                    .AddTransient<IJsonTranslator, JsonTranslator>()
                    .AddEventStore(
                        Configuration.GetSection("EventStoreConnection"),
                        services.BuildServiceProvider().GetService<ILoggerFactory>(),
                        new List<Type>
                        {
                            typeof(ConfigurationBuilt),
                            typeof(DefaultEnvironmentCreated),
                            typeof(EnvironmentCreated),
                            typeof(EnvironmentDeleted),
                            typeof(EnvironmentLayerCreated),
                            typeof(EnvironmentLayerDeleted),
                            typeof(EnvironmentLayerCopied),
                            typeof(EnvironmentLayersModified),
                            typeof(EnvironmentLayerTagsChanged),
                            typeof(EnvironmentLayerKeysImported),
                            typeof(EnvironmentLayerKeysModified),
                            typeof(StructureCreated),
                            typeof(StructureDeleted),
                            typeof(StructureVariablesModified)
                        })
                    .AddHostedService<GracefulShutdownService>()
                    .AddHostedService<TemporaryKeyCleanupService>()
                    .AddHostedService<DomainObjectProjection>()
                    .AddHostedService<ProjectionCacheCleanupService>();
        }

        private void RegisterHealthEndpoints(IServiceCollection services)
        {
            var signalrServer = Configuration.GetSection("EventBusConnection:Server").Get<string>();
            var signalrHub = Configuration.GetSection("EventBusConnection:Hub").Get<string>();

            services.AddSingleton<HttpPipelineCheck>();
            services.AddSingleton<EventStoreClusterCheck>();
            services.AddSingleton<EventStoreConnectionCheck>();
            services.AddSingleton<DomainEventProjectionCheck>();
            services.AddSingleton<ProjectionCacheCompatibleCheck>();
            services.AddSingleton<ProjectionStatusCheck>();
            services.AddHealthChecks()
                    .AddCheck<EventStoreClusterCheck>(
                        "EventStore-ConnectionType",
                        HealthStatus.Unhealthy,
                        new[] { Liveness },
                        TimeSpan.FromSeconds(2))
                    .AddCheck<HttpPipelineCheck>(
                        "Http-Pipeline",
                        HealthStatus.Unhealthy,
                        new[] { Liveness },
                        TimeSpan.FromSeconds(1))
                    .AddCheck<EventStoreConnectionCheck>(
                        "EventStore-Connection",
                        HealthStatus.Unhealthy,
                        new[] { Readiness },
                        TimeSpan.FromSeconds(30))
                    .AddCheck<DomainEventProjectionCheck>(
                        "DomainEventProjection-Subscription",
                        HealthStatus.Unhealthy,
                        new[] { Readiness },
                        TimeSpan.FromSeconds(30))
                    .AddCheck<ProjectionCacheCompatibleCheck>(
                        "ProjectionCache-Compatibility",
                        HealthStatus.Unhealthy,
                        new[] { Readiness },
                        TimeSpan.FromSeconds(1))
                    .AddCheck<ProjectionStatusCheck>(
                        "Projection-Status",
                        HealthStatus.Healthy,
                        new[] { Status },
                        TimeSpan.FromSeconds(1))
                    .AddRedis(
                        Configuration.GetSection("MemoryCache:Redis:ConnectionString").Get<string>(),
                        "Temporary-Keys (Redis)",
                        HealthStatus.Unhealthy,
                        new[] { Readiness })
                    .AddSignalRHub(
                        new Uri(
                                new Uri(signalrServer, UriKind.Absolute),
                                new Uri(signalrHub, UriKind.Relative))
                            .ToString(),
                        "SignalR-Connection",
                        HealthStatus.Unhealthy,
                        new[] { Readiness });
        }

        private void RegisterMvc(IServiceCollection services)
        {
            // setup MVC
            services.AddMvc()
                    .AddJsonOptions(
                        options =>
                        {
                            options.JsonSerializerOptions.AllowTrailingCommas = true;
                            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                        })
                    .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
        }

        private void RegisterOptions(IServiceCollection services)
        {
            // misc stuff
            services.Configure<EventBusConnectionConfiguration>(Configuration.GetSection("EventBusConnection"));
            services.Configure<EventStoreConnectionConfiguration>(Configuration.GetSection("EventStoreConnection"));

            // object-stores
            services.Configure<HistoryConfiguration>(Configuration.GetSection("History"));

            // secret-stores
            services.Configure<ConfiguredSecretStoreConfiguration>(Configuration.GetSection("SecretConfiguration:Stores:Configuration"));
            services.Configure<AzureSecretStoreConfiguration>(Configuration.GetSection("SecretConfiguration:Stores:Azure"));
        }

        private void RegisterSecretStores(IServiceCollection services)
        {
            // define section => func that will be evaluated in order
            var storeRegistrations = new (string Section, Action<IConfigurationSection, IServiceCollection> RegistryFunc)[]
            {
                ("Configuration", RegisterConfiguredSecretStore),
                ("Azure", RegisterAzureSecretStore)
            };

            // look for all enabled stores, and collect some metadata
            List<(string SectionName,
                Action<IConfigurationSection, IServiceCollection> RegistryFunc,
                IConfigurationSection Section,
                bool Enabled)> selectedStores
                = storeRegistrations.Select(
                                        t =>
                                        {
                                            (string section, Action<IConfigurationSection, IServiceCollection> registryFunc) = t;

                                            IConfigurationSection storeSection = Configuration.GetSection($"SecretConfiguration:Stores:{section}");
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
                services.AddScoped<ISecretConfigValueProvider, VoidSecretStore>();
                return;
            }

            selectedStores[0].RegistryFunc(selectedStores[0].Section, services);
        }

        private void RegisterVersioning(IServiceCollection services)
        {
            // setup API-Versioning and Swagger
            services.AddApiVersioning(
                        options =>
                        {
                            options.AssumeDefaultVersionWhenUnspecified = true;
                            options.DefaultApiVersion = new ApiVersion(0, 0);
                            options.ReportApiVersions = true;
                        })
                    .AddVersionedApiExplorer(
                        options =>
                        {
                            options.AssumeDefaultVersionWhenUnspecified = true;
                            options.DefaultApiVersion = new ApiVersion(0, 0);
                            options.GroupNameFormat = "'v'VVV";
                            options.SubstituteApiVersionInUrl = true;
                        });
        }
    }
}
