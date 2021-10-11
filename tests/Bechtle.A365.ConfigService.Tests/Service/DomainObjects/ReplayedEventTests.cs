using System;
using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class ReplayedEventTests
    {
        public static IEnumerable<object[]> Values => new[]
        {
            new object[] { 0, new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")), DateTime.UtcNow },
            new object[] { long.MaxValue, new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")), DateTime.UtcNow },
            new object[] { long.MinValue, new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")), DateTime.UtcNow },
            new object[] { 0, new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")), DateTime.MinValue },
            new object[] { long.MaxValue, new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")), DateTime.MinValue },
            new object[] { long.MinValue, new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")), DateTime.MinValue },
            new object[] { 0, new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")), DateTime.MaxValue },
            new object[] { long.MaxValue, new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")), DateTime.MaxValue },
            new object[] { long.MinValue, new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")), DateTime.MaxValue }
        };

        [Theory]
        [MemberData(nameof(Values))]
        public void AssignValues(long version, DomainEvent domainEvent, DateTime time)
        {
            var replayedEvent = new ReplayedEvent(domainEvent, time, version);

            Assert.Equal(version, replayedEvent.Version);
            Assert.Equal(domainEvent, replayedEvent.DomainEvent);
            Assert.Equal(time, replayedEvent.UtcTime);
        }

        [Fact]
        public void CreateNew() => Assert.NotNull(
            new ReplayedEvent(
                new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                DateTime.UtcNow,
                1));
    }
}
