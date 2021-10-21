using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common.DomainEvents
{
    public class EnvironmentLayerKeysModifiedTests
    {
        public static IEnumerable<object[]> EventData => new[]
        {
            new object[] { "", new[] { ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?"), ConfigKeyAction.Delete("Boo") } },
            new object[] { "", new[] { ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?") } },
            new object[] { "", new[] { ConfigKeyAction.Delete("Boo") } },
            new object[] { "", Array.Empty<ConfigKeyAction>() },
            new object[] { "Bar", new[] { ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?"), ConfigKeyAction.Delete("Boo") } },
            new object[] { "Bar", new[] { ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?") } },
            new object[] { "Bar", new[] { ConfigKeyAction.Delete("Boo") } },
            new object[] { "Bar", Array.Empty<ConfigKeyAction>() }
        };

        [Theory]
        [MemberData(nameof(EventData))]
        public void Equality(string layerName, ConfigKeyAction[] actions)
        {
            var left = new EnvironmentLayerKeysModified(new LayerIdentifier(layerName), actions);
            var right = new EnvironmentLayerKeysModified(new LayerIdentifier(layerName), actions);

            Assert.True(left.Equals(right), "left.Equals(right)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void GetHashCodeStable(string layerName, ConfigKeyAction[] actions)
        {
            var domainEvent = new EnvironmentLayerKeysModified(new LayerIdentifier(layerName), actions);

            List<int> hashes = Enumerable.Range(0, 1000)
                                         .Select(_ => domainEvent.GetHashCode())
                                         .ToList();

            int example = domainEvent.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void MetadataFilled(string layerName, ConfigKeyAction[] actions)
        {
            var domainEvent = new EnvironmentLayerKeysModified(new LayerIdentifier(layerName), actions);

            DomainEventMetadata metadata = domainEvent.GetMetadata();

            Assert.NotEmpty(metadata.Filters);
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperator(string layerName, ConfigKeyAction[] actions)
        {
            var left = new EnvironmentLayerKeysModified(new LayerIdentifier(layerName), actions);
            var right = new EnvironmentLayerKeysModified(new LayerIdentifier(layerName), actions);

            Assert.True(left == right, "left == right");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperatorNegated(string layerName, ConfigKeyAction[] actions)
        {
            var left = new EnvironmentLayerKeysModified(new LayerIdentifier(layerName), actions);
            var right = new EnvironmentLayerKeysModified(new LayerIdentifier(layerName), actions);

            Assert.False(left != right, "left != right");
        }
    }
}
