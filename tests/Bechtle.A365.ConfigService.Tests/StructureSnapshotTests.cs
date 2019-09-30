using System;
using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests
{
    public class StructureSnapshotTests
    {
        [Fact]
        public void DataImmutable()
        {
            var key = "Key1";
            var originalValue = "Value1";

            var snapshot = new StructureSnapshot(new StructureIdentifier("Foo", 42),
                                                 new Dictionary<string, string> {{key, originalValue}},
                                                 new Dictionary<string, string>());

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
            var snapshot = new StructureSnapshot(new StructureIdentifier("Foo", 42), null, new Dictionary<string, string>());

            Assert.NotNull(snapshot.Data);
            Assert.Empty(snapshot.Data);
        }

        [Fact]
        public void NullVariablesReplaced()
        {
            var snapshot = new StructureSnapshot(new StructureIdentifier("Foo", 42), new Dictionary<string, string>(), null);

            Assert.NotNull(snapshot.Variables);
            Assert.Empty(snapshot.Variables);
        }

        [Fact]
        public void ValuesStoredAsExpected()
        {
            var identifier = new StructureIdentifier("Foo", 42);
            var data = new Dictionary<string, string>
            {
                {"Key1", "Value1"}
            };
            var variables = new Dictionary<string, string>
            {
                {"Var1", "Val1"}
            };

            var snapshot = new StructureSnapshot(identifier, data, variables);

            Assert.Equal(identifier, snapshot.Identifier);
            Assert.Equal(data, snapshot.Data);
            Assert.Equal(variables, snapshot.Variables);
        }

        [Fact]
        public void VariablesImmutable()
        {
            var key = "Var1";
            var originalValue = "Val1";

            var snapshot = new StructureSnapshot(new StructureIdentifier("Foo", 42),
                                                 new Dictionary<string, string>(),
                                                 new Dictionary<string, string> {{key, originalValue}});

            try
            {
                snapshot.Variables[key] = "SnapshotShouldBeImmutable";
            }
            catch (Exception)
            {
                // don't care about exceptions here
            }

            Assert.Equal(originalValue, snapshot.Variables[key]);
        }
    }
}