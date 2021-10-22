using System.Collections.Generic;
using System.Linq;
using AutoFixture.Xunit2;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common.DomainEvents
{
    public class EnvironmentLayerTagsChangedTests
    {
        [Theory]
        [AutoData]
        public void Equality(string name)
        {
            var left = new EnvironmentLayerTagsChanged(
                new LayerIdentifier(name),
                new List<string> { "Foo" },
                new List<string> { "Bar" });
            var right = new EnvironmentLayerTagsChanged(
                new LayerIdentifier(name),
                new List<string> { "Foo" },
                new List<string> { "Bar" });

            Assert.True(left.Equals(right), "left.Equals(right)");
        }

        [Theory]
        [AutoData]
        public void GetHashCodeStable(EnvironmentLayerTagsChanged domainEvent)
        {
            List<int> hashes = Enumerable.Range(0, 1000)
                                         .Select(_ => domainEvent.GetHashCode())
                                         .ToList();

            int example = domainEvent.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [AutoData]
        public void MetadataFilled(EnvironmentLayerTagsChanged domainEvent)
        {
            DomainEventMetadata metadata = domainEvent.GetMetadata();

            Assert.NotEmpty(metadata.Filters);
        }

        [Theory]
        [AutoData]
        public void NullCheckOperator(string name)
        {
            var left = new EnvironmentLayerTagsChanged(new LayerIdentifier(name), new List<string>(), new List<string>());
            var right = new EnvironmentLayerTagsChanged(new LayerIdentifier(name), new List<string>(), new List<string>());

            Assert.True(left == right, "left == right");
        }

        [Theory]
        [AutoData]
        public void NullCheckOperatorNegated(EnvironmentLayerTagsChanged left, EnvironmentLayerTagsChanged right)
        {
            Assert.True(left != right, "left != right");
        }
    }
}
