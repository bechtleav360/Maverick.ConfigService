using System.Collections.Generic;
using System.Linq;
using AutoFixture.Xunit2;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common.DomainEvents
{
    public class StructureDeletedTests
    {
        [Theory]
        [AutoData]
        public void Equality(string structName, int structVersion)
        {
            var left = new StructureDeleted(new StructureIdentifier(structName, structVersion));
            var right = new StructureDeleted(new StructureIdentifier(structName, structVersion));

            Assert.True(left.Equals(right), "left.Equals(right)");
        }

        [Theory]
        [AutoData]
        public void GetHashCodeStable(StructureDeleted domainEvent)
        {
            List<int> hashes = Enumerable.Range(0, 1000)
                                         .Select(_ => domainEvent.GetHashCode())
                                         .ToList();

            int example = domainEvent.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [AutoData]
        public void MetadataFilled(StructureDeleted domainEvent)
        {
            DomainEventMetadata metadata = domainEvent.GetMetadata();

            Assert.NotEmpty(metadata.Filters);
        }

        [Theory]
        [AutoData]
        public void NullCheckOperator(string structName, int structVersion)
        {
            var left = new StructureDeleted(new StructureIdentifier(structName, structVersion));
            var right = new StructureDeleted(new StructureIdentifier(structName, structVersion));

            Assert.True(left == right, "left == right");
        }

        [Theory]
        [AutoData]
        public void NullCheckOperatorNegated(StructureDeleted left, StructureDeleted right)
        {
            Assert.True(left != right, "left != right");
        }
    }
}
