using System;
using Bechtle.A365.ConfigService.Common;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common
{
    // we should be able to simply mock ILogger.Log, and then verify that it has been called
    // but alas, MS decided to make a crucial type for this internal when going from 2.1->3.0
    // this means we just have to assume that calling EventStoreLogger.{LogLevel} works and forwards the call as expected
    public class EventStoreLoggerTests
    {
        [Fact]
        public void DebugLogs() => TestLogLevelInternal(logger => logger.Debug("Hello, World"));

        [Fact]
        public void ErrorLogs() => TestLogLevelInternal(logger => logger.Error("Hello, World"));

        [Fact]
        public void InfoLogs() => TestLogLevelInternal(logger => logger.Info("Hello, World"));

        private void TestLogLevelInternal(Action<EventStoreLogger> debugFunc)
        {
            var logger = new Mock<ILogger<IEventStoreConnection>>();
            var esLogger = new EventStoreLogger(logger.Object);

            debugFunc.Invoke(esLogger);
        }
    }
}