﻿using Bechtle.A365.ConfigService.Projection.DataStorage;
using Bechtle.A365.ConfigService.Projection.DomainEventHandlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.EventHandlerTests
{
    public class EnvironmentCreatedHandlerTests
    {
        /// <summary>
        ///     check if the handler can be created using mocks that fail on access
        /// </summary>
        [Fact]
        public void HandlerCreatedWithNonFunctionalValues()
            => Assert.NotNull(new EnvironmentCreatedHandler(
                                  new Mock<IConfigurationDatabase>(MockBehavior.Strict).Object,
                                  new Mock<ILogger<EnvironmentCreatedHandler>>(MockBehavior.Strict).Object));

        /// <summary>
        ///     check if the handler can be created using these options
        /// </summary>
        [Fact]
        public void HandlerCreatedWithoutValues()
            => Assert.NotNull(new EnvironmentCreatedHandler(null, null));
    }
}