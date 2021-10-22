using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture.Xunit2;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common.DomainEvents
{
    public class ConfigurationBuiltTests
    {
        [Theory]
        [AutoData]
        public void Equality(
            string envCategory,
            string envName,
            string structName,
            int structVersion,
            int version,
            DateTime? start,
            DateTime? end)
        {
            var left = new ConfigurationBuilt(
                new ConfigurationIdentifier(
                    new EnvironmentIdentifier(envCategory, envName),
                    new StructureIdentifier(structName, structVersion),
                    version),
                start,
                end);

            var right = new ConfigurationBuilt(
                new ConfigurationIdentifier(
                    new EnvironmentIdentifier(envCategory, envName),
                    new StructureIdentifier(structName, structVersion),
                    version),
                start,
                end);

            Assert.True(left.Equals(right), "left.Equals(right)");
        }

        [Theory]
        [AutoData]
        public void GetHashCodeStable(ConfigurationBuilt domainEvent)
        {
            List<int> hashes = Enumerable.Range(0, 1000)
                                         .Select(_ => domainEvent.GetHashCode())
                                         .ToList();

            int example = domainEvent.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [AutoData]
        public void MetadataFilled(
            string envCategory,
            string envName,
            string structName,
            int structVersion,
            int version,
            DateTime? start,
            DateTime? end)
        {
            var domainEvent = new ConfigurationBuilt(
                new ConfigurationIdentifier(
                    new EnvironmentIdentifier(envCategory, envName),
                    new StructureIdentifier(structName, structVersion),
                    version),
                start,
                end);

            DomainEventMetadata metadata = domainEvent.GetMetadata();

            Assert.NotEmpty(metadata.Filters);
        }

        [Theory]
        [AutoData]
        public void NullCheckOperator(
            string envCategory,
            string envName,
            string structName,
            int structVersion,
            int version,
            DateTime? start,
            DateTime? end)
        {
            var left = new ConfigurationBuilt(
                new ConfigurationIdentifier(
                    new EnvironmentIdentifier(envCategory, envName),
                    new StructureIdentifier(structName, structVersion),
                    version),
                start,
                end);

            var right = new ConfigurationBuilt(
                new ConfigurationIdentifier(
                    new EnvironmentIdentifier(envCategory, envName),
                    new StructureIdentifier(structName, structVersion),
                    version),
                start,
                end);

            Assert.True(left == right, "left == right");
        }

        [Theory]
        [AutoData]
        public void NullCheckOperatorNegated(ConfigurationBuilt left, ConfigurationBuilt right)
        {
            Assert.True(left != right, "left != right");
        }
    }
}
