using System.Collections.Generic;
using System.Linq;
using AutoFixture.Xunit2;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common.DomainEvents
{
    public class DefaultEnvironmentCreatedTests
    {
        [Theory]
        [AutoData]
        public void Equality(string envCategory, string envName)
        {
            var left = new DefaultEnvironmentCreated(new EnvironmentIdentifier(envCategory, envName));
            var right = new DefaultEnvironmentCreated(new EnvironmentIdentifier(envCategory, envName));

            Assert.True(left.Equals(right), "left.Equals(right)");
        }

        [Theory]
        [AutoData]
        public void GetHashCodeStable(DefaultEnvironmentCreated domainEvent)
        {
            List<int> hashes = Enumerable.Range(0, 1000)
                                         .Select(_ => domainEvent.GetHashCode())
                                         .ToList();

            int example = domainEvent.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [AutoData]
        public void MetadataFilled(DefaultEnvironmentCreated domainEvent)
        {
            DomainEventMetadata metadata = domainEvent.GetMetadata();

            Assert.NotEmpty(metadata.Filters);
        }

        [Theory]
        [AutoData]
        public void NullCheckOperator(string envCategory, string envName)
        {
            var left = new DefaultEnvironmentCreated(new EnvironmentIdentifier(envCategory, envName));
            var right = new DefaultEnvironmentCreated(new EnvironmentIdentifier(envCategory, envName));

            Assert.True(left == right, "left == right");
        }

        [Theory]
        [AutoData]
        public void NullCheckOperatorNegated(DefaultEnvironmentCreated left, DefaultEnvironmentCreated right)
        {
            Assert.True(left != right, "left != right");
        }
    }
}
