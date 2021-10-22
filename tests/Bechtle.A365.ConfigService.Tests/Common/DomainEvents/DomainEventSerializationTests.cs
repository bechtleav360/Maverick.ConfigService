using AutoFixture.Xunit2;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Tests.Utilities;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common.DomainEvents
{
    public class SerializationTests : SerializationTestBase
    {
        [Theory]
        [AutoData]
        public void Deserialize_ConfigurationBuilt_Newtonsoft(ConfigurationBuilt domainEvent)
            => TestDeserialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_ConfigurationBuilt_System(ConfigurationBuilt domainEvent)
            => TestDeserialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_DefaultEnvironmentCreated_Newtonsoft(DefaultEnvironmentCreated domainEvent)
            => TestDeserialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_DefaultEnvironmentCreated_System(DefaultEnvironmentCreated domainEvent)
            => TestDeserialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_EnvironmentCreated_Newtonsoft(EnvironmentCreated domainEvent)
            => TestDeserialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_EnvironmentCreated_System(EnvironmentCreated domainEvent)
            => TestDeserialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_EnvironmentDeleted_Newtonsoft(EnvironmentDeleted domainEvent)
            => TestDeserialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_EnvironmentDeleted_System(EnvironmentDeleted domainEvent)
            => TestDeserialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_EnvironmentLayerCopied_Newtonsoft(EnvironmentLayerCopied domainEvent)
            => TestDeserialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_EnvironmentLayerCopied_System(EnvironmentLayerCopied domainEvent)
            => TestDeserialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_EnvironmentLayerKeysImported_Newtonsoft(EnvironmentLayerKeysImported domainEvent)
            => TestDeserialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_EnvironmentLayerKeysImported_System(EnvironmentLayerKeysImported domainEvent)
            => TestDeserialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_EnvironmentLayerKeysModified_Newtonsoft(EnvironmentLayerKeysModified domainEvent)
            => TestDeserialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_EnvironmentLayerKeysModified_System(EnvironmentLayerKeysModified domainEvent)
            => TestDeserialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_EnvironmentLayersModified_Newtonsoft(EnvironmentLayersModified domainEvent)
            => TestDeserialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_EnvironmentLayersModified_System(EnvironmentLayersModified domainEvent)
            => TestDeserialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_EnvironmentLayerTagsChanged_Newtonsoft(EnvironmentLayerTagsChanged domainEvent)
            => TestDeserialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_EnvironmentLayerTagsChanged_System(EnvironmentLayerTagsChanged domainEvent)
            => TestDeserialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_StructureCreated_Newtonsoft(StructureCreated domainEvent)
            => TestDeserialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_StructureCreated_System(StructureCreated domainEvent)
            => TestDeserialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_StructureDeleted_Newtonsoft(StructureDeleted domainEvent)
            => TestDeserialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_StructureDeleted_System(StructureDeleted domainEvent)
            => TestDeserialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_StructureVariablesModified_Newtonsoft(StructureVariablesModified domainEvent)
            => TestDeserialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Deserialize_StructureVariablesModified_System(StructureVariablesModified domainEvent)
            => TestDeserialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_ConfigurationBuilt_Newtonsoft(ConfigurationBuilt domainEvent)
            => TestSerialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_ConfigurationBuilt_System(ConfigurationBuilt domainEvent)
            => TestSerialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_DefaultEnvironmentCreated_Newtonsoft(DefaultEnvironmentCreated domainEvent)
            => TestSerialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_DefaultEnvironmentCreated_System(DefaultEnvironmentCreated domainEvent)
            => TestSerialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_EnvironmentCreated_Newtonsoft(EnvironmentCreated domainEvent)
            => TestSerialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_EnvironmentCreated_System(EnvironmentCreated domainEvent)
            => TestSerialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_EnvironmentDeleted_Newtonsoft(EnvironmentDeleted domainEvent)
            => TestSerialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_EnvironmentDeleted_System(EnvironmentDeleted domainEvent)
            => TestSerialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_EnvironmentLayerCopied_Newtonsoft(EnvironmentLayerCopied domainEvent)
            => TestSerialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_EnvironmentLayerCopied_System(EnvironmentLayerCopied domainEvent)
            => TestSerialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_EnvironmentLayerKeysImported_Newtonsoft(EnvironmentLayerKeysImported domainEvent)
            => TestSerialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_EnvironmentLayerKeysImported_System(EnvironmentLayerKeysImported domainEvent)
            => TestSerialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_EnvironmentLayerKeysModified_Newtonsoft(EnvironmentLayerKeysModified domainEvent)
            => TestSerialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_EnvironmentLayerKeysModified_System(EnvironmentLayerKeysModified domainEvent)
            => TestSerialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_EnvironmentLayersModified_Newtonsoft(EnvironmentLayersModified domainEvent)
            => TestSerialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_EnvironmentLayersModified_System(EnvironmentLayersModified domainEvent)
            => TestSerialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_EnvironmentLayerTagsChanged_Newtonsoft(EnvironmentLayerTagsChanged domainEvent)
            => TestSerialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_EnvironmentLayerTagsChanged_System(EnvironmentLayerTagsChanged domainEvent)
            => TestSerialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_StructureCreated_Newtonsoft(StructureCreated domainEvent)
            => TestSerialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_StructureCreated_System(StructureCreated domainEvent)
            => TestSerialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_StructureDeleted_Newtonsoft(StructureDeleted domainEvent)
            => TestSerialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_StructureDeleted_System(StructureDeleted domainEvent)
            => TestSerialization_System(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_StructureVariablesModified_Newtonsoft(StructureVariablesModified domainEvent)
            => TestSerialization_Newtonsoft(domainEvent);

        [Theory]
        [AutoData]
        public void Serialize_StructureVariablesModified_System(StructureVariablesModified domainEvent)
            => TestSerialization_System(domainEvent);
    }
}
