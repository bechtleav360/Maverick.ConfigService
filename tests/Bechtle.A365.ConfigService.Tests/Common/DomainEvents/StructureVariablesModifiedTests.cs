﻿using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common.DomainEvents
{
    public class StructureVariablesModifiedTests
    {
        public static IEnumerable<object[]> EventData => new[]
        {
            new object[] {null, 0, new ConfigKeyAction[0]},
            new object[] {null, 0, new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?")}},
            new object[] {null, 0, new[] {ConfigKeyAction.Delete("Boo")}},
            new object[] {null, 0, new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?"), ConfigKeyAction.Delete("Boo")}},
            new object[] {null, int.MinValue, new ConfigKeyAction[0]},
            new object[] {null, int.MinValue, new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?")}},
            new object[] {null, int.MinValue, new[] {ConfigKeyAction.Delete("Boo")}},
            new object[] {null, int.MinValue, new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?"), ConfigKeyAction.Delete("Boo")}},
            new object[] {"", 0, new ConfigKeyAction[0]},
            new object[] {"", 0, new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?")}},
            new object[] {"", 0, new[] {ConfigKeyAction.Delete("Boo")}},
            new object[] {"", 0, new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?"), ConfigKeyAction.Delete("Boo")}},
            new object[] {"", int.MinValue, new ConfigKeyAction[0]},
            new object[] {"", int.MinValue, new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?")}},
            new object[] {"", int.MinValue, new[] {ConfigKeyAction.Delete("Boo")}},
            new object[] {"", int.MinValue, new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?"), ConfigKeyAction.Delete("Boo")}},
            new object[] {"", int.MaxValue, new ConfigKeyAction[0]},
            new object[] {"", int.MaxValue, new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?")}},
            new object[] {"", int.MaxValue, new[] {ConfigKeyAction.Delete("Boo")}},
            new object[] {"", int.MaxValue, new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?"), ConfigKeyAction.Delete("Boo")}},
            new object[] {"Baz", 42, new ConfigKeyAction[0]},
            new object[] {"Baz", 42, new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?")}},
            new object[] {"Baz", 42, new[] {ConfigKeyAction.Delete("Boo")}},
            new object[] {"Baz", 42, new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?"), ConfigKeyAction.Delete("Boo")}},
            new object[] {"Baz", int.MaxValue, new ConfigKeyAction[0]},
            new object[] {"Baz", int.MaxValue, new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?")}},
            new object[] {"Baz", int.MaxValue, new[] {ConfigKeyAction.Delete("Boo")}},
            new object[] {"Baz", int.MaxValue, new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?"), ConfigKeyAction.Delete("Boo")}},
            new object[] {"Baz", int.MinValue, new ConfigKeyAction[0]},
            new object[] {"Baz", int.MinValue, new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?")}},
            new object[] {"Baz", int.MinValue, new[] {ConfigKeyAction.Delete("Boo")}},
            new object[] {"Baz", int.MinValue, new[] {ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?"), ConfigKeyAction.Delete("Boo")}},
        };

        [Theory]
        [MemberData(nameof(EventData))]
        public void Equality(string structName, int structVersion, ConfigKeyAction[] actions)
        {
            var left = new StructureVariablesModified(new StructureIdentifier(structName, structVersion), actions);
            var right = new StructureVariablesModified(new StructureIdentifier(structName, structVersion), actions);

            Assert.True(left.Equals(right), "left.Equals(right)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void GetHashCodeStable(string structName, int structVersion, ConfigKeyAction[] actions)
        {
            var domainEvent = new StructureVariablesModified(new StructureIdentifier(structName, structVersion), actions);

            var hashes = Enumerable.Range(0, 1000)
                                   .Select(i => domainEvent.GetHashCode())
                                   .ToList();

            var example = domainEvent.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void MetadataFilled(string structName, int structVersion, ConfigKeyAction[] actions)
        {
            var domainEvent = new StructureVariablesModified(new StructureIdentifier(structName, structVersion), actions);

            var metadata = domainEvent.GetMetadata();

            Assert.NotEmpty(metadata.Filters);
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperator(string structName, int structVersion, ConfigKeyAction[] actions)
        {
            var left = new StructureVariablesModified(new StructureIdentifier(structName, structVersion), actions);
            var right = new StructureVariablesModified(new StructureIdentifier(structName, structVersion), actions);

            Assert.True(left == right, "left == right");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperatorNegated(string structName, int structVersion, ConfigKeyAction[] actions)
        {
            var left = new StructureVariablesModified(new StructureIdentifier(structName, structVersion), actions);
            var right = new StructureVariablesModified(new StructureIdentifier(structName, structVersion), actions);

            Assert.False(left != right, "left != right");
        }
    }
}