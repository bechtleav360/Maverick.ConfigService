using System;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Bechtle.A365.Core.EventBus.Abstraction;
using Bechtle.A365.Core.EventBus.Events.Events;
using Bechtle.A365.Core.EventBus.Events.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Bechtle.A365.ConfigService.Projection.Services
{
    // not using a full-blown scheduler, but one can be found here:
    // https://blog.maartenballiauw.be/post/2017/08/01/building-a-scheduled-cache-updater-in-aspnet-core-2.html
    /// <inheritdoc />
    public class ScheduledConfigPublisherService : HostedService
    {
        /// <inheritdoc />
        public ScheduledConfigPublisherService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ConfigurationIdentifier currentConfiguration = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                using (var scope = GetNewScope())
                {
                    var store = scope.ServiceProvider.GetService<IConfigurationDatabase>();

                    if (currentConfiguration is null)
                    {
                        currentConfiguration = await store.GetLatestActiveConfiguration();
                    }
                    else
                    {
                        var activeConfiguration = await store.GetLatestActiveConfiguration();

                        // if the active and our current configuration differ, trigger a change notification and update our data
                        if (!currentConfiguration.Environment.Category.Equals(activeConfiguration.Environment.Category, StringComparison.OrdinalIgnoreCase) ||
                            !currentConfiguration.Environment.Name.Equals(activeConfiguration.Environment.Name, StringComparison.OrdinalIgnoreCase) ||
                            !currentConfiguration.Structure.Name.Equals(activeConfiguration.Structure.Name, StringComparison.OrdinalIgnoreCase) ||
                            currentConfiguration.Structure.Version != activeConfiguration.Structure.Version)
                        {
                            var eventBus = scope.ServiceProvider.GetService<IEventBus>();

                            await eventBus.Publish(new EventMessage
                            {
                                Event = new OnConfigurationPublished
                                {
                                    EnvironmentCategory = activeConfiguration.Environment.Category,
                                    EnvironmentName = activeConfiguration.Environment.Name,
                                    StructureName = activeConfiguration.Structure.Name,
                                    StructureVersion = activeConfiguration.Structure.Version
                                }
                            });

                            currentConfiguration = activeConfiguration;
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
    }
}