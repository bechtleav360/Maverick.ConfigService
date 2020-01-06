using System.Collections.Generic;
using Bechtle.A365.ConfigService.Configuration;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Configuration
{
    public class ConfigModelTests
    {
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

        [Theory]
        [InlineData("", false, "", "", 0)]
        public void ReadKestrelAuthenticationConfig(string certificate, bool enabled, string address, string password, int port)
        {
            var config = new KestrelAuthenticationConfiguration
            {
                Certificate = certificate,
                Enabled = enabled,
                IpAddress = address,
                Password = password,
                Port = port
            };

            Assert.Equal(certificate, config.Certificate);
            Assert.Equal(enabled, config.Enabled);
            Assert.Equal(address, config.IpAddress);
            Assert.Equal(password, config.Password);
            Assert.Equal(port, config.Port);
        }

        [Fact]
        public void CreateEventBusConnectionConfig() => Assert.NotNull(new EventBusConnectionConfiguration());

        [Fact]
        public void CreateEventStoreConnectionConfig() => Assert.NotNull(new EventStoreConnectionConfiguration());

        [Fact]
        public void CreateKestrelAuthenticationConfig() => Assert.NotNull(new KestrelAuthenticationConfiguration());

        [Fact]
        public void CreateProtectedConfig() => Assert.NotNull(new ProtectedConfiguration());

        [Fact]
        public void FillEventBusConnectionConfig() => Assert.NotNull(new EventBusConnectionConfiguration
        {
            Hub = string.Empty,
            Server = string.Empty
        });

        [Fact]
        public void FillEventStoreConnectionConfig() => Assert.NotNull(new EventStoreConnectionConfiguration
        {
            ConnectionName = string.Empty,
            MaxLiveQueueSize = 42,
            ReadBatchSize = 42,
            Stream = string.Empty,
            Uri = string.Empty
        });

        [Fact]
        public void FillKestrelAuthenticationConfig() => Assert.NotNull(new KestrelAuthenticationConfiguration
        {
            Certificate = string.Empty,
            Enabled = false,
            IpAddress = string.Empty,
            Password = string.Empty,
            Port = 42
        });

        [Fact]
        public void FillProtectedConfig() => Assert.NotNull(new ProtectedConfiguration
        {
            Regions = new Dictionary<string, string>()
        });

        [Fact]
        public void ProtectedConfigCopiesValues()
        {
            var originalDict = new Dictionary<string, string>
            {
                {"Foo", "FooValue"},
                {"Bar", "BarValue"},
                {"Baz", null}
            };

            var protectedConfig = new ProtectedConfiguration {Regions = originalDict};

            Assert.NotSame(originalDict, protectedConfig.Regions);
        }

        [Fact]
        public void ProtectedConfigStoresValues()
        {
            var originalDict = new Dictionary<string, string>
            {
                {"Foo", "FooValue"},
                {"Bar", "BarValue"},
                {"Baz", null}
            };

            var protectedConfig = new ProtectedConfiguration {Regions = originalDict};

            Assert.Equal(originalDict, protectedConfig.Regions);
        }
    }
}