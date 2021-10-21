using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common.DomainEvents
{
    public class EnvironmentDeletedTests
    {
        public static IEnumerable<object[]> EventData => new[]
        {
            new object[] { "", "" },
            new object[] { "Foo", "Bar" }
        };

        [Theory]
        [MemberData(nameof(EventData))]
        public void Equality(string envCategory, string envName)
        {
            var left = new EnvironmentDeleted(new EnvironmentIdentifier(envCategory, envName));
            var right = new EnvironmentDeleted(new EnvironmentIdentifier(envCategory, envName));

            Assert.True(left.Equals(right), "left.Equals(right)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void GetHashCodeStable(string envCategory, string envName)
        {
            var domainEvent = new EnvironmentDeleted(new EnvironmentIdentifier(envCategory, envName));

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
            var domainEvent = new EnvironmentDeleted(new EnvironmentIdentifier(envCategory, envName));

            DomainEventMetadata metadata = domainEvent.GetMetadata();

            Assert.NotEmpty(metadata.Filters);
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperator(string envCategory, string envName)
        {
            var left = new EnvironmentDeleted(new EnvironmentIdentifier(envCategory, envName));
            var right = new EnvironmentDeleted(new EnvironmentIdentifier(envCategory, envName));

            Assert.True(left == right, "left == right");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperatorNegated(string envCategory, string envName)
        {
            var left = new EnvironmentDeleted(new EnvironmentIdentifier(envCategory, envName));
            var right = new EnvironmentDeleted(new EnvironmentIdentifier(envCategory, envName));

            Assert.False(left != right, "left != right");
        }
    }
}
