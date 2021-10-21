using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class PreparedConfigurationTests
    {
        [Fact]
        public void CreateNew()
            => Assert.NotNull(
                new PreparedConfiguration(
                    new ConfigurationIdentifier(
                        new EnvironmentIdentifier("Foo", "Bar"),
                        new StructureIdentifier("Foo", 1),
                        2)));
    }
}
