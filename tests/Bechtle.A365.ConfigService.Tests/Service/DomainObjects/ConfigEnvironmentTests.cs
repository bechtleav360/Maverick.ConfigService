using System;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class ConfigEnvironmentTests
    {
        [Fact]
        public void CreateNew() => Assert.NotNull(new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar")));
    }
}
