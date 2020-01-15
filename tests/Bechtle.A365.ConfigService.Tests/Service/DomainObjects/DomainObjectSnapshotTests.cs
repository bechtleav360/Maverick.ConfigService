using System.Collections.Generic;
using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class DomainObjectSnapshotTests
    {
        public static IEnumerable<object[]> Values => new[]
        {
            new object[] {string.Empty, string.Empty, string.Empty, 0, 0},
            new object[] {string.Empty, string.Empty, string.Empty, long.MinValue, long.MinValue},
            new object[] {string.Empty, string.Empty, string.Empty, long.MaxValue, long.MaxValue},
            new object[] {null, null, null, 0, 0},
            new object[] {null, null, null, long.MinValue, long.MinValue},
            new object[] {null, null, null, long.MaxValue, long.MaxValue},
            new object[] {"Foo", "Bar", "Baz", 0, 0},
            new object[] {"Foo", "Bar", "Baz", long.MinValue, long.MinValue},
            new object[] {"Foo", "Bar", "Baz", long.MaxValue, long.MaxValue}
        };

        [Theory]
        [MemberData(nameof(Values))]
        public void AssignValues(string dataType, string identifier, string jsonData, long metaVersion, long version)
        {
            var snapshot = new DomainObjectSnapshot(dataType, identifier, jsonData, version, metaVersion);

            Assert.Equal(dataType, snapshot.DataType);
            Assert.Equal(identifier, snapshot.Identifier);
            Assert.Equal(jsonData, snapshot.JsonData);
            Assert.Equal(metaVersion, snapshot.MetaVersion);
            Assert.Equal(version, snapshot.Version);
        }
    }
}