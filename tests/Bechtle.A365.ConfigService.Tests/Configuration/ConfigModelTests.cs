using Bechtle.A365.ConfigService.Configuration;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Configuration
{
    public class ConfigModelTests
    {
        [Fact]
        public void CreateEventBusConnectionConfig() => Assert.NotNull(new EventBusConnectionConfiguration());

        [Fact]
        public void CreateEventStoreConnectionConfig() => Assert.NotNull(new EventStoreConnectionConfiguration());

        [Fact]
        public void FillEventBusConnectionConfig() => Assert.NotNull(
            new EventBusConnectionConfiguration
            {
                Hub = string.Empty,
                Server = string.Empty
            });

        [Fact]
        public void FillEventStoreConnectionConfig() => Assert.NotNull(
            new EventStoreConnectionConfiguration
            {
                ConnectionName = string.Empty,
                MaxLiveQueueSize = 42,
                ReadBatchSize = 42,
                Stream = string.Empty,
                Uri = string.Empty
            });

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("Foo", "Bar")]
        public void ReadEventBusConnectionConfig(string hub, string server)
        {
            var config = new EventBusConnectionConfiguration
            {
                Hub = hub,
                Server = server
            };

            Assert.Equal(hub, config.Hub);
            Assert.Equal(server, config.Server);
        }

        [Theory]
        [InlineData("", 0, 0, "", "")]
        [InlineData(null, 0, 0, null, null)]
        [InlineData(null, -1, -1, null, null)]
        [InlineData("Foo", 42, 4711, "Bar", "Baz")]
        public void ReadEventStoreConnectionConfig(string name, int queueSize, int batchSize, string stream, string uri)
        {
            var config = new EventStoreConnectionConfiguration
            {
                ConnectionName = name,
                MaxLiveQueueSize = queueSize,
                ReadBatchSize = batchSize,
                Stream = stream,
                Uri = uri
            };

            Assert.Equal(name, config.ConnectionName);
            Assert.Equal(queueSize, config.MaxLiveQueueSize);
            Assert.Equal(batchSize, config.ReadBatchSize);
            Assert.Equal(stream, config.Stream);
            Assert.Equal(uri, config.Uri);
        }
    }
}
