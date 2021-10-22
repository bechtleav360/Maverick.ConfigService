using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class EnvironmentLayerTests
    {
        [Fact]
        public void CreateNew() => Assert.NotNull(new EnvironmentLayer(new LayerIdentifier("Foo")));
    }
}
