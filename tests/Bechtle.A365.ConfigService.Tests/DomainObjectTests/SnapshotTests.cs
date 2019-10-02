using System;
using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.DomainObjectTests
{
    public class SnapshotTests
    {
        public static IEnumerable<object[]> TestDates => new[]
        {
            new object[] {DateTime.MinValue},
            new object[] {DateTime.MaxValue},
            new object[] {DateTime.UnixEpoch},
            new object[] {new DateTime()},
            new object[] {null}
        };

        [Fact]
        public void Create() => new ConfigSnapshot().Create();

        [Fact]
        public void IdentifiedBy() => new ConfigSnapshot().IdentifiedBy(new StructureIdentifier("Foo", 42), new EnvironmentIdentifier("Foo", "Bar"));

        [Theory]
        [MemberData(nameof(TestDates))]
        public void ValidFrom(DateTime? dt) => new ConfigSnapshot().ValidFrom(dt);

        [Theory]
        [MemberData(nameof(TestDates))]
        public void ValidTo(DateTime? dt) => new ConfigSnapshot().ValidTo(dt);
    }
}