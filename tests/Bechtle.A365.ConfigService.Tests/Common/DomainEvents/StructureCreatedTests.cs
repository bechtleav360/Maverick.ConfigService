using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common.DomainEvents
{
    public class StructureCreatedTests
    {
        public static IEnumerable<object[]> EventData => new[]
        {
            new object[] { null, 0, new Dictionary<string, string>(), new Dictionary<string, string>() },
            new object[] { null, 0, new Dictionary<string, string> { { "", "" } }, new Dictionary<string, string>() },
            new object[] { null, 0, new Dictionary<string, string>(), new Dictionary<string, string> { { "", "" } } },
            new object[] { null, 0, new Dictionary<string, string> { { "", "" } }, new Dictionary<string, string> { { "", "" } } },
            new object[] { null, int.MinValue, new Dictionary<string, string>(), new Dictionary<string, string>() },
            new object[] { null, int.MinValue, new Dictionary<string, string> { { "", "" } }, new Dictionary<string, string>() },
            new object[] { null, int.MinValue, new Dictionary<string, string>(), new Dictionary<string, string> { { "", "" } } },
            new object[] { null, int.MinValue, new Dictionary<string, string> { { "", "" } }, new Dictionary<string, string> { { "", "" } } },
            new object[] { "", 0, new Dictionary<string, string>(), new Dictionary<string, string>() },
            new object[] { "", 0, new Dictionary<string, string> { { "", "" } }, new Dictionary<string, string>() },
            new object[] { "", 0, new Dictionary<string, string>(), new Dictionary<string, string> { { "", "" } } },
            new object[] { "", 0, new Dictionary<string, string> { { "", "" } }, new Dictionary<string, string> { { "", "" } } },
            new object[] { "", int.MinValue, new Dictionary<string, string>(), new Dictionary<string, string>() },
            new object[] { "", int.MinValue, new Dictionary<string, string> { { "", "" } }, new Dictionary<string, string>() },
            new object[] { "", int.MinValue, new Dictionary<string, string>(), new Dictionary<string, string> { { "", "" } } },
            new object[] { "", int.MinValue, new Dictionary<string, string> { { "", "" } }, new Dictionary<string, string> { { "", "" } } },
            new object[] { "", int.MaxValue, new Dictionary<string, string>(), new Dictionary<string, string>() },
            new object[] { "", int.MaxValue, new Dictionary<string, string> { { "", "" } }, new Dictionary<string, string>() },
            new object[] { "", int.MaxValue, new Dictionary<string, string>(), new Dictionary<string, string> { { "", "" } } },
            new object[] { "", int.MaxValue, new Dictionary<string, string> { { "", "" } }, new Dictionary<string, string> { { "", "" } } },
            new object[] { "Baz", 42, new Dictionary<string, string>(), new Dictionary<string, string>() },
            new object[] { "Baz", 42, new Dictionary<string, string> { { "", "" } }, new Dictionary<string, string>() },
            new object[] { "Baz", 42, new Dictionary<string, string>(), new Dictionary<string, string> { { "", "" } } },
            new object[] { "Baz", 42, new Dictionary<string, string> { { "", "" } }, new Dictionary<string, string> { { "", "" } } },
            new object[] { "Baz", int.MaxValue, new Dictionary<string, string>(), new Dictionary<string, string>() },
            new object[] { "Baz", int.MaxValue, new Dictionary<string, string> { { "", "" } }, new Dictionary<string, string>() },
            new object[] { "Baz", int.MaxValue, new Dictionary<string, string>(), new Dictionary<string, string> { { "", "" } } },
            new object[] { "Baz", int.MaxValue, new Dictionary<string, string> { { "", "" } }, new Dictionary<string, string> { { "", "" } } },
            new object[] { "Baz", int.MinValue, new Dictionary<string, string>(), new Dictionary<string, string>() },
            new object[] { "Baz", int.MinValue, new Dictionary<string, string> { { "", "" } }, new Dictionary<string, string>() },
            new object[] { "Baz", int.MinValue, new Dictionary<string, string>(), new Dictionary<string, string> { { "", "" } } },
            new object[] { "Baz", int.MinValue, new Dictionary<string, string> { { "", "" } }, new Dictionary<string, string> { { "", "" } } }
        };

        [Theory]
        [MemberData(nameof(EventData))]
        public void Equality(string structName, int structVersion, Dictionary<string, string> keys, Dictionary<string, string> variables)
        {
            var left = new StructureCreated(new StructureIdentifier(structName, structVersion), keys, variables);
            var right = new StructureCreated(new StructureIdentifier(structName, structVersion), keys, variables);

            Assert.True(left.Equals(right), "left.Equals(right)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void GetHashCodeStable(string structName, int structVersion, Dictionary<string, string> keys, Dictionary<string, string> variables)
        {
            var domainEvent = new StructureCreated(new StructureIdentifier(structName, structVersion), keys, variables);

            List<int> hashes = Enumerable.Range(0, 1000)
                                         .Select(i => domainEvent.GetHashCode())
                                         .ToList();

            int example = domainEvent.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void MetadataFilled(string structName, int structVersion, Dictionary<string, string> keys, Dictionary<string, string> variables)
        {
            var domainEvent = new StructureCreated(new StructureIdentifier(structName, structVersion), keys, variables);

            DomainEventMetadata metadata = domainEvent.GetMetadata();

            Assert.NotEmpty(metadata.Filters);
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperator(string structName, int structVersion, Dictionary<string, string> keys, Dictionary<string, string> variables)
        {
            var left = new StructureCreated(new StructureIdentifier(structName, structVersion), keys, variables);
            var right = new StructureCreated(new StructureIdentifier(structName, structVersion), keys, variables);

            Assert.True(left == right, "left == right");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperatorNegated(string structName, int structVersion, Dictionary<string, string> keys, Dictionary<string, string> variables)
        {
            var left = new StructureCreated(new StructureIdentifier(structName, structVersion), keys, variables);
            var right = new StructureCreated(new StructureIdentifier(structName, structVersion), keys, variables);

            Assert.False(left != right, "left != right");
        }
    }
}
