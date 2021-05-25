﻿using System;
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
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
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

namespace Bechtle.A365.ConfigService
{
    /// <summary>
    ///     Service-Startup behaviour
    /// </summary>
    public class Startup : DefaultStartup
    {
        private const string Liveness = "Liveness";
        private const string Readiness = "Readiness";
        private ILogger<Startup> _logger;

        /// <inheritdoc cref="Startup" />
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration) : base(configuration)
        {
        }

        /// <inheritdoc />
        protected override void AddEarlyConfiguration(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            IApiVersionDescriptionProvider provider)
        {
            _logger = app.ApplicationServices.GetRequiredService<ILogger<Startup>>();
        }

        /// <inheritdoc />
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
        {
            var appConfiguration = app.StartTweakingWith(_logger, Configuration);

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

                              foreach (var description in provider.ApiVersionDescriptions.OrderByDescending(v => v.ApiVersion))
                                  options.SwaggerEndpoint(
                                      $"/swagger/{description.GroupName}/swagger.json",
                                      $"ConfigService {description.GroupName.ToUpperInvariant()}");
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
                            .Tweak(a => a.UseEndpoints(builder => builder.MapHealthChecksUI()), "adding /healthchecks-ui endpoint");

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
        protected override void AddServiceConfiguration(IServiceCollection services)
        {
            RegisterOptions(services);
            RegisterMvc(services);
            RegisterVersioning(services);
            RegisterSwagger(services);
            RegisterDiServices(services);
            RegisterSecretStores(services);
            RegisterHealthEndpoints(services);
        }

        private void RegisterAzureSecretStore(IConfigurationSection section, IServiceCollection services)
            => services.AddScoped<ISecretConfigValueProvider, AzureSecretStore>(_logger);

        private void RegisterConfiguredSecretStore(IConfigurationSection section, IServiceCollection services)
            => services.AddScoped<ISecretConfigValueProvider, ConfiguredSecretStore>(_logger);

        private void RegisterDiServices(IServiceCollection services)
        {
            _logger.LogInformation("Registering App-Services");

            // setup services for DI
            services.AddMemoryCache(
                        options =>
                        {
                            var sizeInMb = Configuration.GetSection("MemoryCache:Local:SizeLimitInMb").Get<long>();
                            var sizeLimit = sizeInMb * 1024 * 1024;
                            var compactionPercentage = Configuration.GetSection("MemoryCache:Local:CompactionPercentage").Get<double>();

                            _logger.LogInformation($"configuring IMemoryCache, SizeLimit='{sizeLimit}', CompactionPercentage='{compactionPercentage}'");

                            options.SizeLimit = sizeLimit;
                            options.CompactionPercentage = compactionPercentage;
                        })
                    .AddStackExchangeRedisCache(
                        options =>
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
                    .AddTransient<IDataExporter, DataExporter>(_logger)
                    .AddTransient<IDataImporter, DataImporter>(_logger)
                    .AddTransient<IEventBus, WebSocketEventBusClient>(
                        _logger,
                        provider =>
                        {
                            var config = provider.GetRequiredService<IOptionsMonitor<EventBusConnectionConfiguration>>().CurrentValue;

                            if (string.IsNullOrWhiteSpace(config.Server))
                                throw new ArgumentException("EventBusConfiguration.Server is null or empty");

                            if (string.IsNullOrWhiteSpace(config.Hub))
                                throw new ArgumentException("EventBusConfiguration.Hub is null or empty");

                            return new WebSocketEventBusClient(
                                new Uri(new Uri(config.Server), config.Hub).ToString(),
                                provider.GetService<ILoggerFactory>());
                        })
                    .AddTransient<ICommandValidator, InternalDataCommandValidator>(_logger)
                    .AddTransient<IDomainObjectStore, DomainObjectStore>(_logger)
                    .AddSingleton<IJsonTranslator, JsonTranslator>(_logger)
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
                            typeof(EnvironmentLayerKeysImported),
                            typeof(EnvironmentLayerKeysModified),
                            typeof(StructureCreated),
                            typeof(StructureDeleted),
                            typeof(StructureVariablesModified),
                        })
                    .AddHostedService<GracefulShutdownService>(_logger)
                    .AddHostedService<TemporaryKeyCleanupService>(_logger);
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
                    .AddRedis(
                        Configuration.GetSection("MemoryCache:Redis:ConnectionString").Get<string>(),
                        "Temporary-Keys (Redis)",
                        HealthStatus.Unhealthy,
                        new[] {Readiness})
                    .AddSignalRHub(
                        new Uri(
                                new Uri(signalrServer, UriKind.Absolute),
                                new Uri(signalrHub, UriKind.Relative))
                            .ToString(),
                        "SignalR-Connection",
                        HealthStatus.Unhealthy,
                        new[] {Readiness});

            services.AddHealthChecksUI(
                        setup =>
                        {
                            setup.DisableDatabaseMigrations();
                            setup.AddHealthCheckEndpoint("Ready Checks", "/health/ready");
                            setup.AddHealthCheckEndpoint("Live Checks", "/health/live");
                        })
                    .AddInMemoryStorage();
        }

        private void RegisterMvc(IServiceCollection services)
        {
            _logger.LogInformation("registering MVC-Middleware with metrics-support");

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
            services.Configure<KestrelAuthenticationConfiguration>(Configuration.GetSection("Authentication:Kestrel"));
            services.Configure<EventBusConnectionConfiguration>(Configuration.GetSection("EventBusConnection"));
            services.Configure<ProtectedConfiguration>(Configuration.GetSection("Protection"));
            services.Configure<EventStoreConnectionConfiguration>(Configuration.GetSection("EventStoreConnection"));

            // secret-stores
            services.Configure<ConfiguredSecretStoreConfiguration>(Configuration.GetSection("SecretConfiguration:Stores:Configuration"));
            services.Configure<AzureSecretStoreConfiguration>(Configuration.GetSection("SecretConfiguration:Stores:Azure"));
        }

        private void RegisterSecretStores(IServiceCollection services)
        {
            var storeBaseSection = "SecretConfiguration:Stores";

            // define section => func that will be evaluated in order
            var storeRegistrations = new (string Section, Action<IConfigurationSection, IServiceCollection> RegistryFunc)[]
            {
                ("Configuration", RegisterConfiguredSecretStore),
                ("Azure", RegisterAzureSecretStore)
            };

            _logger.LogInformation(
                $"looking for an enabled SecretStore ({storeBaseSection}:{{Store}}:[Enabled]) "
                + $"in this order ({string.Join(", ", storeRegistrations.Select(t => $"{t.Section}"))})");

            // look for all enabled stores, and collect some metadata
            var selectedStores = storeRegistrations.Select(
                                                       t =>
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
                _logger.LogWarning($"no actual secret-stores have been registered, using {nameof(VoidSecretStore)} as fallback");
                services.AddScoped<ISecretConfigValueProvider, VoidSecretStore>(_logger);
                return;
            }

            if (selectedStores.Count > 1)
                _logger.LogError(
                    "multiple stores have been enabled ("
                    + string.Join(", ", selectedStores.Select(t => t.SectionName))
                    + $"), but only one will be registered ({selectedStores.First().SectionName})");

            selectedStores[0].RegistryFunc?.Invoke(selectedStores[0].Section, services);
        }

        private void RegisterSwagger(IServiceCollection services)
        {
            _logger.LogInformation("registering Swagger");

            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>()
                    .AddSwaggerGen(
                        options =>
                        {
                            options.CustomSchemaIds(t => t.FullName);

                            var ass = Assembly.GetEntryAssembly();
                            if (!(ass is null))
                                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{ass.GetName().Name}.xml"));
                        });
        }

        private void RegisterVersioning(IServiceCollection services)
        {
            _logger.LogInformation("registering API-Version support");

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
