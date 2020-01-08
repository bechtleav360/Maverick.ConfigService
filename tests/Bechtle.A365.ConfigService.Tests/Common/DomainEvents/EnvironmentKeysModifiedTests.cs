using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common.DomainEvents
{
    public class EnvironmentKeysModifiedTests
    {
        public static IEnumerable<object[]> EventData => new[]
        {
            new object[] {null, null, new ConfigKeyAction[0]},
            new object[] {"", "", new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?"), ConfigKeyAction.Delete("Boo")}},
            new object[] {"", "", new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?")}},
            new object[] {"", "", new[] {ConfigKeyAction.Delete("Boo")}},
            new object[] {"", "", new ConfigKeyAction[0]},
            new object[] {"Foo", "Bar", new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?"), ConfigKeyAction.Delete("Boo")}},
            new object[] {"Foo", "Bar", new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?")}},
            new object[] {"Foo", "Bar", new[] {ConfigKeyAction.Delete("Boo")}},
            new object[] {"Foo", "Bar", new ConfigKeyAction[0]}
        };

        [Theory]
        [MemberData(nameof(EventData))]
        public void Equality(string envCategory, string envName, ConfigKeyAction[] actions)
        {
            var left = new EnvironmentKeysModified(new EnvironmentIdentifier(envCategory, envName), actions);
            var right = new EnvironmentKeysModified(new EnvironmentIdentifier(envCategory, envName), actions);

            Assert.True(left.Equals(right), "left.Equals(right)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void GetHashCodeStable(string envCategory, string envName, ConfigKeyAction[] actions)
        {
            var domainEvent = new EnvironmentKeysModified(new EnvironmentIdentifier(envCategory, envName), actions);

            var hashes = Enumerable.Range(0, 1000)
                                   .Select(i => domainEvent.GetHashCode())
                                   .ToList();

            var example = domainEvent.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void MetadataFilled(string envCategory, string envName, ConfigKeyAction[] actions)
        {
            var domainEvent = new EnvironmentKeysModified(new EnvironmentIdentifier(envCategory, envName), actions);

            var metadata = domainEvent.GetMetadata();

            Assert.NotEmpty(metadata.Filters);
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperator(string envCategory, string envName, ConfigKeyAction[] actions)
        {
            var left = new EnvironmentKeysModified(new EnvironmentIdentifier(envCategory, envName), actions);
            var right = new EnvironmentKeysModified(new EnvironmentIdentifier(envCategory, envName), actions);

            Assert.True(left == right, "left == right");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperatorNegated(string envCategory, string envName, ConfigKeyAction[] actions)
        {
            var left = new EnvironmentKeysModified(new EnvironmentIdentifier(envCategory, envName), actions);
            var right = new EnvironmentKeysModified(new EnvironmentIdentifier(envCategory, envName), actions);

            Assert.False(left != right, "left != right");
        }
    }
}