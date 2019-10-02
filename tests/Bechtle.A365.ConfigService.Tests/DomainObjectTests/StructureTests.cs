using System.Collections.Generic;
using AutoFixture.Xunit2;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.DomainObjectTests
{
    public class StructureTests
    {
        public static IEnumerable<object[]> IdentifiedByParameters() => new[]
        {
            new object[] {new StructureIdentifier(string.Empty, 0)},
            new object[] {new StructureIdentifier(string.Empty, 0)},
            new object[] {new StructureIdentifier("Foo", 42)},
            new object[] {new StructureIdentifier("Foo", 42)},
            new object[] {new StructureIdentifier(null, -1)},
            new object[] {new StructureIdentifier(null, -1)},
            new object[] {new StructureIdentifier(null, 0)},
            new object[] {new StructureIdentifier(null, 0)},
            new object[] {new StructureIdentifier(string.Empty, -1)},
            new object[] {new StructureIdentifier(string.Empty, -1)}
        };

        [Theory]
        [MemberData(nameof(IdentifiedByParameters))]
        public void IdentifiedBy(StructureIdentifier identifier)
            => new ConfigStructure().IdentifiedBy(identifier);

        [Theory]
        [AutoData]
        public void ModifyVariables(IList<ConfigKeyAction> keys) => new ConfigStructure().ModifyVariables(keys);

        [Fact]
        public void Create() => new ConfigStructure().Create(new Dictionary<string, string>(), new Dictionary<string, string>());

        [Fact]
        public void Delete() => new ConfigStructure().Delete();

        [Fact]
        public void ModifyNoVariables() => new ConfigStructure().ModifyVariables(new ConfigKeyAction[0]);

        [Fact]
        public void ModifyNullVariables() => new ConfigStructure().ModifyVariables(null);
    }
}