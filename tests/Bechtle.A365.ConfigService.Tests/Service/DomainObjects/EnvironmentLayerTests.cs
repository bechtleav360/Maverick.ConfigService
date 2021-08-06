using System;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class EnvironmentLayerTests
    {
        [Fact]
        public void CreateNew() => Assert.NotNull(new EnvironmentLayer(new LayerIdentifier("Foo")));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ThrowsForInvalidIdentifier(string name) => Assert.Throws<ArgumentNullException>(
            () => new EnvironmentLayer(new LayerIdentifier(name)));

        [Fact]
        public void ThrowsForNullIdentifier() => Assert.Throws<ArgumentNullException>(() => new EnvironmentLayer(null));
    }
}
