using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common
{
    public class EnvironmentIdentifierTests
    {
        [Theory]
        [InlineData("", "", "", "")]
        [InlineData(null, null, null, null)]
        [InlineData("category", "name", "CATEGORY", "NAME")]
        [InlineData("fooBar", "foobar", "FOOBAR", "fOObAR")]
        public void CaseInsensitivity(string categoryLeft, string nameLeft, string categoryRight, string nameRight)
            => Assert.True(new EnvironmentIdentifier(categoryLeft, nameLeft).Equals(new EnvironmentIdentifier(categoryRight, nameRight)),
                           "new EnvironmentIdentifier(categoryLeft, nameLeft).Equals(new EnvironmentIdentifier(categoryRight, nameRight))");

        [Theory]
        [InlineData("", "")]
        [InlineData(null, "")]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void EmptyValues(string category, string name) => Assert.NotNull(new EnvironmentIdentifier(category, name));

        // obviously 'new object is null' is false, but this tests if the operator== has issues
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        [Fact]
        public void NullCheckOperator() => Assert.False(new EnvironmentIdentifier("Foo", "Bar") == null,
                                                        "new EnvironmentIdentifier('Foo', 'Bar') == null");

        // same behaviour as NullCheckOperator
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        [Fact]
        public void NullCheckOperatorNegated() => Assert.True(new EnvironmentIdentifier("Foo", "Bar") != null,
                                                              "new EnvironmentIdentifier('Foo', 'Bar') != null");

        [Theory]
        [InlineData("", "")]
        [InlineData(null, "")]
        [InlineData("", null)]
        [InlineData(null, null)]
        [InlineData("FooBar", "BarBaz")]
        public void GetHashCodeStable(string category, string name)
        {
            var identifier = new EnvironmentIdentifier(category, name);

            var hashes = Enumerable.Range(0, 1000)
                                   .Select(i => identifier.GetHashCode())
                                   .ToList();

            var example = identifier.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [InlineData("", "")]
        [InlineData(null, "")]
        [InlineData("", null)]
        [InlineData(null, null)]
        [InlineData("Foo", "Bar")]
        public void NoToStringExceptions(string category, string name) => Assert.NotNull(new EnvironmentIdentifier(category, name).ToString());
    }
}