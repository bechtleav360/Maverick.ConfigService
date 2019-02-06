﻿using System;
using System.IO;
using System.Reflection;
using Bechtle.A365.ConfigService.Authentication.Certificates;
using Bechtle.A365.ConfigService.Authentication.Certificates.Events;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseMvc()
               .UseAuthentication()
               .UseCors(policy => policy.AllowAnyHeader()
                                        .AllowAnyMethod()
                                        .AllowAnyOrigin())
               .UseSwagger()
               .UseSwaggerUI(options =>
               {
                   options.SwaggerEndpoint("/swagger/v2/swagger.json", string.Empty);
                   options.DocExpansion(DocExpansion.None);
                   options.DisplayRequestDuration();
               });
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
                    .AddSingleton(provider => provider.GetService<IConfiguration>().Get<ConfigServiceConfiguration>())
                    .AddSingleton(provider => provider.GetService<ConfigServiceConfiguration>().EventBusConnection)
                    .AddSingleton(provider => provider.GetService<ConfigServiceConfiguration>().EventStoreConnection)
                    .AddSingleton(provider => provider.GetService<ConfigServiceConfiguration>().ProjectionStorage)
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
                    .AddSingleton<IEventStore, Services.EventStore>()
                    .AddSingleton<ESLogger, EventStoreLogger>()
                    .AddSingleton<IJsonTranslator, JsonTranslator>()
                    .AddSingleton<IEventDeserializer, EventDeserializer>()
                    .AddSingleton(typeof(IDomainEventConverter<>), typeof(DomainEventConverter<>));
        }
    }
}