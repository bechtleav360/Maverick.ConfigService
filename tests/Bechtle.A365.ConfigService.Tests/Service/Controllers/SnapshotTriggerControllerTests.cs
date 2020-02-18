using System;
using Bechtle.A365.ConfigService.Controllers.V1;
using Bechtle.A365.ConfigService.Implementations.SnapshotTriggers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.Controllers
{
    public class SnapshotTriggerControllerTests : ControllerTests<SnapshotTriggerController>
    {
        /// <inheritdoc />
        protected override SnapshotTriggerController CreateController()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection()
                                                          .Build();

            var provider = new ServiceCollection().AddLogging()
                                                  .AddMetrics()
                                                  .AddSingleton<IConfiguration>(configuration)
                                                  .BuildServiceProvider();

            return new SnapshotTriggerController(
                provider,
                provider.GetService<ILogger<SnapshotTriggerController>>());
        }

        [Fact]
        public void TriggerSnapshot()
        {
            using var service = new OnDemandSnapshotTrigger();

            // closure is never null because Assert.Raises<T> will execute
            // 'attach' and 'detach' before and after 'testCode' respectively, before returning
            // ReSharper disable AccessToDisposedClosure
            Assert.Raises<EventArgs>(handler => service.SnapshotTriggered += handler,
                                     handler => service.SnapshotTriggered -= handler,
                                     () => { TestAction<AcceptedResult>(c => c.TriggerSnapshot()); });
            // ReSharper restore AccessToDisposedClosure
        }
    }
}