using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common.DomainEvents
{
    public class EnvironmentLayerCopiedTests
    {
        public static IEnumerable<object[]> EventData => new[]
        {
            new object[] { "" },
            new object[] { "Foo" }
        };

        [Theory]
        [MemberData(nameof(EventData))]
        public void Equality(string name)
        {
            var left = new EnvironmentLayerCopied(new LayerIdentifier(name), new LayerIdentifier(name));
            var right = new EnvironmentLayerCopied(new LayerIdentifier(name), new LayerIdentifier(name));

            Assert.True(left.Equals(right), "left.Equals(right)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void GetHashCodeStable(string name)
        {
            var domainEvent = new EnvironmentLayerCopied(new LayerIdentifier(name), new LayerIdentifier(name));

            List<int> hashes = Enumerable.Range(0, 1000)
                                         .Select(_ => domainEvent.GetHashCode())
                                         .ToList();

            int example = domainEvent.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void MetadataFilled(string name)
        {
            var domainEvent = new EnvironmentLayerCopied(new LayerIdentifier(name), new LayerIdentifier(name));

            DomainEventMetadata metadata = domainEvent.GetMetadata();

            Assert.NotEmpty(metadata.Filters);
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperator(string name)
        {
            var left = new EnvironmentLayerCopied(new LayerIdentifier(name), new LayerIdentifier(name));
            var right = new EnvironmentLayerCopied(new LayerIdentifier(name), new LayerIdentifier(name));

            Assert.True(left == right, "left == right");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperatorNegated(string name)
        {
            var left = new EnvironmentLayerCopied(new LayerIdentifier(name), new LayerIdentifier(name));
            var right = new EnvironmentLayerCopied(new LayerIdentifier(name), new LayerIdentifier(name));

            Assert.False(left != right, "left != right");
        }
    }
}
