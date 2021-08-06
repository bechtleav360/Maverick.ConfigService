using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common.DomainEvents
{
    public class EnvironmentLayerTagsChangedTests
    {
        public static IEnumerable<object[]> EventData => new[]
        {
            new object[] { null },
            new object[] { "" },
            new object[] { "Foo" }
        };

        [Theory]
        [MemberData(nameof(EventData))]
        public void Equality(string name)
        {
            var left = new EnvironmentLayerTagsChanged(new LayerIdentifier(name), new List<string>(), new List<string>());
            var right = new EnvironmentLayerTagsChanged(new LayerIdentifier(name), new List<string>(), new List<string>());

            Assert.True(left.Equals(right), "left.Equals(right)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void GetHashCodeStable(string name)
        {
            var domainEvent = new EnvironmentLayerTagsChanged(new LayerIdentifier(name), new List<string>(), new List<string>());

            List<int> hashes = Enumerable.Range(0, 1000)
                                         .Select(i => domainEvent.GetHashCode())
                                         .ToList();

            int example = domainEvent.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void MetadataFilled(string name)
        {
            var domainEvent = new EnvironmentLayerTagsChanged(new LayerIdentifier(name), new List<string>(), new List<string>());

            DomainEventMetadata metadata = domainEvent.GetMetadata();

            Assert.NotEmpty(metadata.Filters);
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperator(string name)
        {
            var left = new EnvironmentLayerTagsChanged(new LayerIdentifier(name), new List<string>(), new List<string>());
            var right = new EnvironmentLayerTagsChanged(new LayerIdentifier(name), new List<string>(), new List<string>());

            Assert.True(left == right, "left == right");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperatorNegated(string name)
        {
            var left = new EnvironmentLayerTagsChanged(new LayerIdentifier(name), new List<string>(), new List<string>());
            var right = new EnvironmentLayerTagsChanged(new LayerIdentifier(name), new List<string>(), new List<string>());

            Assert.False(left != right, "left != right");
        }
    }
}
