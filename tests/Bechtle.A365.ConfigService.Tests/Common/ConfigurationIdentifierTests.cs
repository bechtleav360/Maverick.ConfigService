using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common
{
    public class ConfigurationIdentifierTests
    {
        // obviously 'new object is null' is false, but this tests if the operator== has issues
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        [Fact]
        public void NullCheckOperator()
            => Assert.False(new ConfigurationIdentifier(new EnvironmentIdentifier("Foo", "Bar"), new StructureIdentifier("Baz", 42), 4711) == null,
                            "new ConfigurationIdentifier(new EnvironmentIdentifier('Foo', 'Bar'), new StructureIdentifier('Baz', 42), 4711) == null");

        // same behaviour as NullCheckOperator
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        [Fact]
        public void NullCheckOperatorNegated()
            => Assert.True(new ConfigurationIdentifier(new EnvironmentIdentifier("Foo", "Bar"), new StructureIdentifier("Baz", 42), 4711) != null,
                           "new ConfigurationIdentifier(new EnvironmentIdentifier('Foo', 'Bar'), new StructureIdentifier('Baz', 42), 4711) != null");

        [Theory]
        [InlineData(null, null, null, 0, 0)]
        [InlineData(null, null, null, int.MaxValue, int.MaxValue)]
        [InlineData(null, null, null, int.MinValue, int.MinValue)]
        [InlineData(null, null, null, int.MinValue, int.MaxValue)]
        [InlineData(null, null, null, int.MaxValue, int.MinValue)]
        [InlineData("Foo", "Bar", "Baz", 42, 4711)]
        [InlineData("Foo", "Bar", "Baz", int.MaxValue, int.MaxValue)]
        [InlineData("Foo", "Bar", "Baz", int.MinValue, int.MinValue)]
        [InlineData("Foo", "Bar", "Baz", int.MinValue, int.MaxValue)]
        [InlineData("Foo", "Bar", "Baz", int.MaxValue, int.MinValue)]
        public void GetHashCodeStable(string envCategory, string envName, string structName, int structVersion, int version)
        {
            var identifier = new ConfigurationIdentifier(new EnvironmentIdentifier(envCategory, envName),
                                                         new StructureIdentifier(structName, structVersion),
                                                         version);

            var hashes = Enumerable.Range(0, 1000)
                                   .Select(i => identifier.GetHashCode())
                                   .ToList();

            var example = identifier.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [InlineData(null, null, null, 0, 0)]
        [InlineData(null, null, null, int.MaxValue, int.MaxValue)]
        [InlineData(null, null, null, int.MinValue, int.MinValue)]
        [InlineData(null, null, null, int.MinValue, int.MaxValue)]
        [InlineData(null, null, null, int.MaxValue, int.MinValue)]
        [InlineData("Foo", "Bar", "Baz", 42, 4711)]
        [InlineData("Foo", "Bar", "Baz", int.MaxValue, int.MaxValue)]
        [InlineData("Foo", "Bar", "Baz", int.MinValue, int.MinValue)]
        [InlineData("Foo", "Bar", "Baz", int.MinValue, int.MaxValue)]
        [InlineData("Foo", "Bar", "Baz", int.MaxValue, int.MinValue)]
        public void NoToStringExceptions(string envCategory, string envName, string structName, int structVersion, int version)
            => Assert.NotNull(new ConfigurationIdentifier(new EnvironmentIdentifier(envCategory, envName),
                                                          new StructureIdentifier(structName, structVersion),
                                                          version).ToString());
    }
}