using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common.DomainEvents
{
    public class StructureDeletedTests
    {
        public static IEnumerable<object[]> EventData => new[]
        {
            new object[] { null, 0 },
            new object[] { null, int.MinValue },
            new object[] { "", 0 },
            new object[] { "", int.MinValue },
            new object[] { "", int.MaxValue },
            new object[] { "Baz", 42 },
            new object[] { "Baz", int.MaxValue },
            new object[] { "Baz", int.MinValue }
        };

        [Theory]
        [MemberData(nameof(EventData))]
        public void Equality(string structName, int structVersion)
        {
            var left = new StructureDeleted(new StructureIdentifier(structName, structVersion));
            var right = new StructureDeleted(new StructureIdentifier(structName, structVersion));

            Assert.True(left.Equals(right), "left.Equals(right)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void GetHashCodeStable(string structName, int structVersion)
        {
            var domainEvent = new StructureDeleted(new StructureIdentifier(structName, structVersion));

            List<int> hashes = Enumerable.Range(0, 1000)
                                         .Select(_ => domainEvent.GetHashCode())
                                         .ToList();

            int example = domainEvent.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void MetadataFilled(string structName, int structVersion)
        {
            var domainEvent = new StructureDeleted(new StructureIdentifier(structName, structVersion));

            DomainEventMetadata metadata = domainEvent.GetMetadata();

            Assert.NotEmpty(metadata.Filters);
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperator(string structName, int structVersion)
        {
            var left = new StructureDeleted(new StructureIdentifier(structName, structVersion));
            var right = new StructureDeleted(new StructureIdentifier(structName, structVersion));

            Assert.True(left == right, "left == right");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperatorNegated(string structName, int structVersion)
        {
            var left = new StructureDeleted(new StructureIdentifier(structName, structVersion));
            var right = new StructureDeleted(new StructureIdentifier(structName, structVersion));

            Assert.False(left != right, "left != right");
        }
    }
}
