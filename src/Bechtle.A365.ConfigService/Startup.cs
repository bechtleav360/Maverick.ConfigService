using System;
using System.IO;
using System.Reflection;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Bechtle.A365.ConfigService.Dto.EventFactories;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

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
            services.AddMvc()
                    .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Info
                {
                    Title = "Bechtle.A365.ConfigService",
                    Version = "V2.0"
                });
                options.DescribeAllEnumsAsStrings();
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetEntryAssembly().GetName().Name}.xml"));
            });

            services.AddSingleton<IConfigStore, ConfigStore>()
                    .AddSingleton<IDomainEventSerializer<EnvironmentCreated>, EnvironmentCreatedSerializer>()
                    .AddSingleton<IDomainEventSerializer<EnvironmentUpdated>, EnvironmentUpdatedSerializer>()
                    .AddSingleton<IDomainEventSerializer<VersionCompiled>, VersionCompiledSerializer>()
                    .AddSingleton<IDomainEventSerializer<SchemaCreated>, SchemaCreatedSerializer>()
                    .AddSingleton<IDomainEventSerializer<SchemaUpdated>, SchemaUpdatedSerializer>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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

            app.UseMvc();

            app.UseSwagger()
               .UseSwaggerUI(options =>
               {
                   options.SwaggerEndpoint("/swagger/v1/swagger.json", string.Empty);
                   options.DocExpansion(DocExpansion.None);
                   options.DisplayRequestDuration();
               });
        }
    }
}