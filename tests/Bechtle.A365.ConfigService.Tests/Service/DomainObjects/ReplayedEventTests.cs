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
            new object[] {0, new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")), DateTime.UtcNow},
            new object[] {long.MaxValue, new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")), DateTime.UtcNow},
            new object[] {long.MinValue, new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")), DateTime.UtcNow},
            new object[] {0, new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")), DateTime.MinValue},
            new object[] {long.MaxValue, new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")), DateTime.MinValue},
            new object[] {long.MinValue, new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")), DateTime.MinValue},
            new object[] {0, new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")), DateTime.MaxValue},
            new object[] {long.MaxValue, new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")), DateTime.MaxValue},
            new object[] {long.MinValue, new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")), DateTime.MaxValue}
        };

        [Fact]
        public void CreateNewBasic() => Assert.NotNull(new ReplayedEvent());

        [Fact]
        public void CreateNew() => Assert.NotNull(new ReplayedEvent
        {
            Version = 1,
            DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
            UtcTime = DateTime.UtcNow
        });

        [Theory]
        [MemberData(nameof(Values))]
        public void AssignValues(long version, DomainEvent domainEvent, DateTime time)
        {
            var @event = new ReplayedEvent
            {
                Version = version,
                DomainEvent = domainEvent,
                UtcTime = time
            };

            Assert.Equal(version, @event.Version);
            Assert.Equal(domainEvent, @event.DomainEvent);
            Assert.Equal(time, @event.UtcTime);
        }
    }
}