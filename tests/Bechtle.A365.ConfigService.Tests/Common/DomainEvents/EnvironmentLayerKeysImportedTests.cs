using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common.DomainEvents
{
    public class EnvironmentLayerKeysImportedTests
    {
        public static IEnumerable<object[]> EventData => new[]
        {
            new object[] {null, new ConfigKeyAction[0]},
            new object[] {"", new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?"), ConfigKeyAction.Delete("Boo")}},
            new object[] {"", new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?")}},
            new object[] {"", new[] {ConfigKeyAction.Delete("Boo")}},
            new object[] {"", new ConfigKeyAction[0]},
            new object[] {"Bar", new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?"), ConfigKeyAction.Delete("Boo")}},
            new object[] {"Bar", new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?")}},
            new object[] {"Bar", new[] {ConfigKeyAction.Delete("Boo")}},
            new object[] {"Bar", new ConfigKeyAction[0]}
        };

        [Theory]
        [MemberData(nameof(EventData))]
        public void Equality(string envName, ConfigKeyAction[] actions)
        {
            var left = new EnvironmentLayerKeysImported(new LayerIdentifier(envName), actions);
            var right = new EnvironmentLayerKeysImported(new LayerIdentifier(envName), actions);

            Assert.True(left.Equals(right), "left.Equals(right)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void GetHashCodeStable(string envName, ConfigKeyAction[] actions)
        {
            var domainEvent = new EnvironmentLayerKeysImported(new LayerIdentifier(envName), actions);

            var hashes = Enumerable.Range(0, 1000)
                                   .Select(i => domainEvent.GetHashCode())
                                   .ToList();

            var example = domainEvent.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void MetadataFilled(string envName, ConfigKeyAction[] actions)
        {
            var domainEvent = new EnvironmentLayerKeysImported(new LayerIdentifier(envName), actions);

            var metadata = domainEvent.GetMetadata();

            Assert.NotEmpty(metadata.Filters);
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperator(string envName, ConfigKeyAction[] actions)
        {
            var left = new EnvironmentLayerKeysImported(new LayerIdentifier(envName), actions);
            var right = new EnvironmentLayerKeysImported(new LayerIdentifier(envName), actions);

            Assert.True(left == right, "left == right");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperatorNegated(string envName, ConfigKeyAction[] actions)
        {
            var left = new EnvironmentLayerKeysImported(new LayerIdentifier(envName), actions);
            var right = new EnvironmentLayerKeysImported(new LayerIdentifier(envName), actions);

            Assert.False(left != right, "left != right");
        }
    }
}