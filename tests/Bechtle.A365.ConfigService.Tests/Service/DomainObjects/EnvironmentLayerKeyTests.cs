using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class EnvironmentLayerKeyTests
    {
        [Fact]
        public void CreateNew() => Assert.NotNull(new EnvironmentLayerKey("", "", "", "", 0));

        [Theory]
        [InlineData("", "", "", "", long.MinValue)]
        [InlineData("", "", "", "", 0)]
        [InlineData("", "", "", "", 4711)]
        [InlineData("", "", "", "", long.MaxValue)]
        [InlineData(null, null, null, null, long.MaxValue)]
        [InlineData(null, null, null, null, 0)]
        [InlineData(null, null, null, null, 4711)]
        [InlineData(null, null, null, null, long.MinValue)]
        [InlineData("desc", "foo", "bar", "baz", long.MaxValue)]
        [InlineData("desc", "foo", "bar", "baz", 4711)]
        [InlineData("desc", "foo", "bar", "baz", 0)]
        [InlineData("desc", "foo", "bar", "baz", long.MinValue)]
        public void AssignValues(string description, string key, string type, string value, long version)
        {
            var item = new EnvironmentLayerKey(key, value, type, description, version);

            Assert.Equal(description, item.Description);
            Assert.Equal(key, item.Key);
            Assert.Equal(type, item.Type);
            Assert.Equal(value, item.Value);
            Assert.Equal(item.Version, item.Version);
        }
    }
}