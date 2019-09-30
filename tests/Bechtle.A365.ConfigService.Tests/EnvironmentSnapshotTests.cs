using System;
using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests
{
    public class EnvironmentSnapshotTests
    {
        [Fact]
        public void DataImmutable()
        {
            var key = "Key1";
            var originalValue = "Value1";

            var snapshot = new EnvironmentSnapshot(new EnvironmentIdentifier("Foo", "Bar"),
                                                   new Dictionary<string, string> {{key, originalValue}});

            try
            {
                snapshot.Data[key] = "SnapshotShouldBeImmutable";
            }
            catch (Exception)
            {
                // don't care about exceptions here
            }

            Assert.Equal(originalValue, snapshot.Data[key]);
        }

        [Fact]
        public void NullDataReplaced()
        {
            var snapshot = new EnvironmentSnapshot(new EnvironmentIdentifier("Foo", "Bar"), null);

            Assert.NotNull(snapshot.Data);
            Assert.Empty(snapshot.Data);
        }

        [Fact]
        public void ValuesStoredAsExpected()
        {
            var identifier = new EnvironmentIdentifier("Foo", "Bar");
            var data = new Dictionary<string, string>
            {
                {"Key1", "Value1"}
            };

            var snapshot = new EnvironmentSnapshot(identifier, data);

            Assert.Equal(identifier, snapshot.Identifier);
            Assert.Equal(data, snapshot.Data);
        }
    }
}