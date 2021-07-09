using System;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class ConfigEnvironmentTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("", "Bar")]
        [InlineData("Foo", "")]
        public void ThrowsForInvalidIdentifier(string category, string name) => Assert.Throws<ArgumentNullException>(
            () => new ConfigEnvironment(new EnvironmentIdentifier(category, name)));

        [Fact]
        public void CreateNew() => Assert.NotNull(new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar")));

        [Fact]
        public void ThrowsForNullIdentifier() => Assert.Throws<ArgumentNullException>(() => new ConfigEnvironment(null));
    }
}
