using System;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class PreparedConfigurationTests
    {
        [Fact]
        public void ThrowForInvalidIdentifier()
            => Assert.Throws<ArgumentNullException>(() => new PreparedConfiguration(new ConfigurationIdentifier(null, null, 0)));

        [Fact]
        public void ThrowForNullIdentifier() => Assert.Throws<ArgumentNullException>(() => new PreparedConfiguration(null));
    }
}
