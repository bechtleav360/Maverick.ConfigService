using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common.DomainEvents
{
    public class EnvironmentLayersModifiedTests
    {
        public static IEnumerable<object[]> EventData => new[]
        {
            new object[] { null, null },
            new object[] { "", "" },
            new object[] { "", null },
            new object[] { null, "" },
            new object[] { "Foo", "Bar" }
        };

        [Theory]
        [MemberData(nameof(EventData))]
        public void Equality(string envCategory, string envName)
        {
            var left = new EnvironmentLayersModified(
                new EnvironmentIdentifier(envCategory, envName),
                new[] { new LayerIdentifier("Foo") });
            var right = new EnvironmentLayersModified(
                new EnvironmentIdentifier(envCategory, envName),
                new[] { new LayerIdentifier("Foo") });

            Assert.True(left.Equals(right), "left.Equals(right)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void GetHashCodeStable(string envCategory, string envName)
        {
            var domainEvent = new EnvironmentLayersModified(
                new EnvironmentIdentifier(envCategory, envName),
                new[] { new LayerIdentifier("Foo") });

            List<int> hashes = Enumerable.Range(0, 1000)
                                         .Select(_ => domainEvent.GetHashCode())
                                         .ToList();

            int example = domainEvent.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void MetadataFilled(string envCategory, string envName)
        {
            var domainEvent = new EnvironmentLayersModified(
                new EnvironmentIdentifier(envCategory, envName),
                new[] { new LayerIdentifier("Foo") });

            DomainEventMetadata metadata = domainEvent.GetMetadata();

            Assert.NotEmpty(metadata.Filters);
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperator(string envCategory, string envName)
        {
            var left = new EnvironmentLayersModified(
                new EnvironmentIdentifier(envCategory, envName),
                new[] { new LayerIdentifier("Foo") });
            var right = new EnvironmentLayersModified(
                new EnvironmentIdentifier(envCategory, envName),
                new[] { new LayerIdentifier("Foo") });

            Assert.True(left == right, "left == right");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperatorNegated(string envCategory, string envName)
        {
            var left = new EnvironmentLayersModified(
                new EnvironmentIdentifier(envCategory, envName),
                new[] { new LayerIdentifier("Foo") });
            var right = new EnvironmentLayersModified(
                new EnvironmentIdentifier(envCategory, envName),
                new[] { new LayerIdentifier("Foo") });

            Assert.False(left != right, "left != right");
        }
    }
}
