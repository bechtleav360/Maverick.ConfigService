using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common.DomainEvents
{
    public class DomainEventMetadataTests
    {
        [Fact]
        public void AccessUnavailableMetadata()
        {
            var metadata = new DomainEventMetadata();

            Assert.Equal(string.Empty, metadata["Foo"]);
        }

        [Fact]
        public void CreateMetadata() => Assert.NotNull(new DomainEventMetadata());

        [Fact]
        public void FillMetadata()
        {
            var metadata = new DomainEventMetadata {["Foo"] = "Bar"};

            Assert.Equal("Bar", metadata["Foo"]);
        }

        [Fact]
        public void FiltersNotNull() => Assert.NotNull(new DomainEventMetadata().Filters);

        [Fact]
        public void IndexerCaseInsensitive()
        {
            var metadata = new DomainEventMetadata {["Foo"] = "Bar"};

            Assert.Equal("Bar", metadata["foo"]);
        }
    }
}