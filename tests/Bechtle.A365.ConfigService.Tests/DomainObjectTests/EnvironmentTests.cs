using System.Collections.Generic;
using App.Metrics;
using AutoFixture.Xunit2;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Dto;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.DomainObjectTests
{
    public class EnvironmentTests
    {
        public static IEnumerable<object[]> IdentifiedByParameters() => new[]
        {
            new object[] {new EnvironmentIdentifier(string.Empty, string.Empty), false},
            new object[] {new EnvironmentIdentifier(string.Empty, string.Empty), true},
            new object[] {new EnvironmentIdentifier("Foo", "Bar"), false},
            new object[] {new EnvironmentIdentifier("Foo", "Bar"), true},
            new object[] {new EnvironmentIdentifier(null, null), false},
            new object[] {new EnvironmentIdentifier(null, null), true},
            new object[] {new EnvironmentIdentifier(null, string.Empty), true},
            new object[] {new EnvironmentIdentifier(null, string.Empty), false},
            new object[] {new EnvironmentIdentifier(string.Empty, null), true},
            new object[] {new EnvironmentIdentifier(string.Empty, null), false}
        };

        [Theory]
        [MemberData(nameof(IdentifiedByParameters))]
        public void IdentifiedBy(EnvironmentIdentifier identifier, bool isDefault)
            => new ConfigEnvironment().IdentifiedBy(identifier, isDefault);

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("_")]
        [InlineData(":")]
        [InlineData(null)]
        [InlineData("Foo")]
        public void DefaultIdentifiedBy(string category)
            => new ConfigEnvironment().DefaultIdentifiedBy(category);

        [Theory]
        [AutoData]
        public void ImportKeys(IEnumerable<DtoConfigKey> keys) => new ConfigEnvironment().ImportKeys(keys);

        [Theory]
        [AutoData]
        public void ModifyKeys(IEnumerable<ConfigKeyAction> keys) => new ConfigEnvironment().ModifyKeys(keys);

        [Fact]
        public void Create() => new ConfigEnvironment().Create();

        [Fact]
        public void Delete() => new ConfigEnvironment().Delete();

        [Fact]
        public void ImportNoKeys() => new ConfigEnvironment().ImportKeys(new DtoConfigKey[0]);

        [Fact]
        public void ImportNullKeys() => new ConfigEnvironment().ImportKeys(null);

        [Fact]
        public void ModifyNoKeys() => new ConfigEnvironment().ModifyKeys(new ConfigKeyAction[0]);

        [Fact]
        public void ModifyNullKeys() => new ConfigEnvironment().ModifyKeys(null);
    }
}