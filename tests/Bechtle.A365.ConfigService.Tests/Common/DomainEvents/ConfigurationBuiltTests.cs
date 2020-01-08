using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common.DomainEvents
{
    public class ConfigurationBuiltTests
    {
        public static IEnumerable<object[]> EventData => new[]
        {
            new object[] {null, null, null, 0, 0, null, null},
            new object[] {"", "", "", 0, 0, new DateTime(2000, 1, 2, 3, 4, 5, 678), new DateTime(2020, 1, 2, 3, 4, 5, 678)},
            new object[] {"", "", "", 0, 0, new DateTime(), new DateTime()},
            new object[] {"Foo", "Bar", "Baz", 42, 4711, new DateTime(), new DateTime()},
            new object[] {"Foo", "Bar", "Baz", 42, 4711, null, null},
        };

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperator(string envCategory,
                                      string envName,
                                      string structName,
                                      int structVersion,
                                      int version,
                                      DateTime start,
                                      DateTime end)
        {
            var left = new ConfigurationBuilt(new ConfigurationIdentifier(
                                                  new EnvironmentIdentifier(envCategory, envName),
                                                  new StructureIdentifier(structName, structVersion),
                                                  version),
                                              start,
                                              end);

            var right = new ConfigurationBuilt(new ConfigurationIdentifier(
                                                   new EnvironmentIdentifier(envCategory, envName),
                                                   new StructureIdentifier(structName, structVersion),
                                                   version),
                                               start,
                                               end);

            Assert.True(left == right, "left == right");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void NullCheckOperatorNegated(string envCategory,
                                             string envName,
                                             string structName,
                                             int structVersion,
                                             int version,
                                             DateTime start,
                                             DateTime end)
        {
            var left = new ConfigurationBuilt(new ConfigurationIdentifier(
                                                  new EnvironmentIdentifier(envCategory, envName),
                                                  new StructureIdentifier(structName, structVersion),
                                                  version),
                                              start,
                                              end);

            var right = new ConfigurationBuilt(new ConfigurationIdentifier(
                                                   new EnvironmentIdentifier(envCategory, envName),
                                                   new StructureIdentifier(structName, structVersion),
                                                   version),
                                               start,
                                               end);

            Assert.False(left != right, "left != right");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void GetHashCodeStable(string envCategory,
                                      string envName,
                                      string structName,
                                      int structVersion,
                                      int version,
                                      DateTime start,
                                      DateTime end)
        {
            var domainEvent = new ConfigurationBuilt(new ConfigurationIdentifier(
                                                         new EnvironmentIdentifier(envCategory, envName),
                                                         new StructureIdentifier(structName, structVersion),
                                                         version),
                                                     start,
                                                     end);

            var hashes = Enumerable.Range(0, 1000)
                                   .Select(i => domainEvent.GetHashCode())
                                   .ToList();

            var example = domainEvent.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void Equality(string envCategory,
                             string envName,
                             string structName,
                             int structVersion,
                             int version,
                             DateTime start,
                             DateTime end)
        {
            var left = new ConfigurationBuilt(new ConfigurationIdentifier(
                                                  new EnvironmentIdentifier(envCategory, envName),
                                                  new StructureIdentifier(structName, structVersion),
                                                  version),
                                              start,
                                              end);

            var right = new ConfigurationBuilt(new ConfigurationIdentifier(
                                                   new EnvironmentIdentifier(envCategory, envName),
                                                   new StructureIdentifier(structName, structVersion),
                                                   version),
                                               start,
                                               end);

            Assert.True(left.Equals(right), "left.Equals(right)");
        }

        [Theory]
        [MemberData(nameof(EventData))]
        public void MetadataFilled(string envCategory,
                                   string envName,
                                   string structName,
                                   int structVersion,
                                   int version,
                                   DateTime start,
                                   DateTime end)
        {
            var domainEvent = new ConfigurationBuilt(new ConfigurationIdentifier(
                                                         new EnvironmentIdentifier(envCategory, envName),
                                                         new StructureIdentifier(structName, structVersion),
                                                         version),
                                                     start,
                                                     end);

            var metadata = domainEvent.GetMetadata();

            Assert.NotEmpty(metadata.Filters);
        }
    }
}