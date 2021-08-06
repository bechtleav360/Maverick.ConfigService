using System;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class ConfigStructureTests
    {
        [Fact]
        public void CreateNew() => Assert.NotNull(new ConfigStructure(new StructureIdentifier("Foo", 42)));

        [Theory]
        [InlineData("FooBar", 0)]
        [InlineData("FooBar", -1)]
        [InlineData("FooBar", int.MinValue)]
        [InlineData(null, int.MaxValue)]
        [InlineData(null, 1)]
        [InlineData(null, 0)]
        [InlineData(null, -1)]
        [InlineData(null, int.MinValue)]
        [InlineData("", int.MaxValue)]
        [InlineData("", 1)]
        [InlineData("", 0)]
        [InlineData("", -1)]
        [InlineData("", int.MinValue)]
        public void ThrowsForInvalidIdentifier(string name, int version) => Assert.Throws<ArgumentNullException>(
            () => new ConfigStructure(new StructureIdentifier(name, version)));
    }
}
