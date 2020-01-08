using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common
{
    public class StructureIdentifierTests
    {
        [Theory]
        [InlineData("", "")]
        [InlineData("foobar", "FOOBAR")]
        [InlineData("fooBar", "fooBar")]
        public void CaseInsensitivity(string string1, string string2)
            => Assert.True(new StructureIdentifier(string1, 42).Equals(new StructureIdentifier(string2, 42)),
                           "new StructureIdentifier(string1, 42).Equals(new StructureIdentifier(string2, 42))");

        [Theory]
        [InlineData("", 0)]
        [InlineData(null, 0)]
        [InlineData("", -1)]
        [InlineData(null, -1)]
        [InlineData(null, int.MaxValue)]
        [InlineData(null, int.MinValue)]
        [InlineData("", int.MaxValue)]
        [InlineData("", int.MinValue)]
        public void EmptyValues(string name, int version) => Assert.NotNull(new StructureIdentifier(name, version));

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void ExtremeVersions(int version) => Assert.NotNull(new StructureIdentifier(string.Empty, version));

        // obviously 'new object is null' is false, but this tests if the operator== has issues
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        [Fact]
        public void NullCheckOperator() => Assert.False(new StructureIdentifier("Foo", 42) == null,
                                                        "new StructureIdentifier('Foo', 42) == null");

        // same behaviour as NullCheckOperator
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        [Fact]
        public void NullCheckOperatorNegated() => Assert.True(new StructureIdentifier("Foo", 42) != null,
                                                              "new StructureIdentifier('Foo', 42) != null");

        [Theory]
        [InlineData("", 42)]
        [InlineData("", -1)]
        [InlineData("", int.MinValue)]
        [InlineData("", int.MaxValue)]
        [InlineData(null, 42)]
        [InlineData(null, -1)]
        [InlineData(null, int.MinValue)]
        [InlineData(null, int.MaxValue)]
        [InlineData("Foo", 42)]
        [InlineData("bar", -1)]
        [InlineData("Foo", int.MinValue)]
        [InlineData("Foo", int.MaxValue)]
        public void GetHashCodeStable(string name, int version)
        {
            var identifier = new StructureIdentifier(name, version);

            var hashes = Enumerable.Range(0, 1000)
                                   .Select(i => identifier.GetHashCode())
                                   .ToList();

            var example = identifier.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [InlineData("", 42)]
        [InlineData("", -1)]
        [InlineData("", int.MinValue)]
        [InlineData("", int.MaxValue)]
        [InlineData(null, 42)]
        [InlineData(null, -1)]
        [InlineData(null, int.MinValue)]
        [InlineData(null, int.MaxValue)]
        [InlineData("Foo", 42)]
        [InlineData("bar", -1)]
        [InlineData("Foo", int.MinValue)]
        [InlineData("Foo", int.MaxValue)]
        public void NoToStringExceptions(string name, int version) => Assert.NotNull(new StructureIdentifier(name, version).ToString());
    }
}