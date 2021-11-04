using System.Collections.Generic;
using System.Text.Json;
using AutoFixture;
using AutoFixture.Kernel;
using AutoFixture.Xunit2;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Tests.Utilities;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common
{
    public class SerializationTests : SerializationTestBase
    {
        [Theory]
        [AutoData]
        public void Deserialize_ConfigExport_Newtonsoft(ConfigExport obj)
            => TestDeserialization_Newtonsoft(obj);

        [Theory]
        [AutoData]
        public void Deserialize_ConfigExport_System(ConfigExport obj)
            => TestDeserialization_System(obj);

        [Theory]
        [AutoData]
        public void Deserialize_ConfigKeyAction_Newtonsoft(ConfigKeyAction action)
            => TestDeserialization_Newtonsoft(action);

        [Theory]
        [AutoData]
        public void Deserialize_ConfigKeyAction_System(ConfigKeyAction action)
            => TestDeserialization_System(action);

        [Theory]
        [AutoData]
        public void Deserialize_ConfigurationBuildOptions_Newtonsoft(ConfigurationBuildOptions obj)
            => TestDeserialization_Newtonsoft(obj);

        [Theory]
        [AutoData]
        public void Deserialize_ConfigurationBuildOptions_System(ConfigurationBuildOptions obj)
            => TestDeserialization_System(obj);

        [Theory]
        [AutoData]
        public void Deserialize_DtoConfigKey_Newtonsoft(DtoConfigKey obj)
            => TestDeserialization_Newtonsoft(obj);

        [Theory]
        [AutoData]
        public void Deserialize_DtoConfigKey_System(DtoConfigKey obj)
            => TestDeserialization_System(obj);

        [Theory]
        [AutoData]
        public void Deserialize_DtoConfigKeyCompletion_Newtonsoft(DtoConfigKeyCompletion obj)
            => TestDeserialization_Newtonsoft(obj);

        [Theory]
        [AutoData]
        public void Deserialize_DtoConfigKeyCompletion_System(DtoConfigKeyCompletion obj)
            => TestDeserialization_System(obj);

        [Fact]
        public void Deserialize_DtoStructure_System()
        {
            var fixture = new Fixture();
            fixture.Customizations.Add(
                new SpecimenFactory<DtoStructure>(
                    () => new DtoStructure
                    {
                        Name = "Foo",
                        Version = 42,
                        Structure = JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement,
                        Variables = new Dictionary<string, object>()
                    }));
            var obj = fixture.Create<DtoStructure>();

            TestDeserialization_System(obj);
        }

        [Theory]
        [AutoData]
        public void Deserialize_EnvironmentExport_Newtonsoft(EnvironmentExport obj)
            => TestDeserialization_Newtonsoft(obj);

        [Theory]
        [AutoData]
        public void Deserialize_EnvironmentExport_System(EnvironmentExport obj)
            => TestDeserialization_System(obj);

        [Theory]
        [AutoData]
        public void Deserialize_EnvironmentKeyExport_Newtonsoft(EnvironmentKeyExport obj)
            => TestDeserialization_Newtonsoft(obj);

        [Theory]
        [AutoData]
        public void Deserialize_EnvironmentKeyExport_System(EnvironmentKeyExport obj)
            => TestDeserialization_System(obj);

        [Theory]
        [AutoData]
        public void Deserialize_ExportDefinition_Newtonsoft(ExportDefinition obj)
            => TestDeserialization_Newtonsoft(obj);

        [Theory]
        [AutoData]
        public void Deserialize_ExportDefinition_System(ExportDefinition obj)
            => TestDeserialization_System(obj);

        [Theory]
        [AutoData]
        public void Deserialize_LayerExport_Newtonsoft(LayerExport obj)
            => TestDeserialization_Newtonsoft(obj);

        [Theory]
        [AutoData]
        public void Deserialize_LayerExport_System(LayerExport obj)
            => TestDeserialization_System(obj);

        [Theory]
        [AutoData]
        public void Serialize_ConfigExport_Newtonsoft(ConfigExport obj)
            => TestSerialization_Newtonsoft(obj);

        [Theory]
        [AutoData]
        public void Serialize_ConfigExport_System(ConfigExport obj)
            => TestSerialization_System(obj);

        [Theory]
        [AutoData]
        public void Serialize_ConfigKeyAction_Newtonsoft(ConfigKeyAction action)
            => TestSerialization_Newtonsoft(action);

        [Theory]
        [AutoData]
        public void Serialize_ConfigKeyAction_System(ConfigKeyAction action)
            => TestSerialization_System(action);

        [Theory]
        [AutoData]
        public void Serialize_ConfigurationBuildOptions_Newtonsoft(ConfigurationBuildOptions obj)
            => TestSerialization_Newtonsoft(obj);

        [Theory]
        [AutoData]
        public void Serialize_ConfigurationBuildOptions_System(ConfigurationBuildOptions obj)
            => TestSerialization_System(obj);

        [Theory]
        [AutoData]
        public void Serialize_DtoConfigKey_Newtonsoft(DtoConfigKey obj)
            => TestSerialization_Newtonsoft(obj);

        [Theory]
        [AutoData]
        public void Serialize_DtoConfigKey_System(DtoConfigKey obj)
            => TestSerialization_System(obj);

        [Theory]
        [AutoData]
        public void Serialize_DtoConfigKeyCompletion_Newtonsoft(DtoConfigKeyCompletion obj)
            => TestSerialization_Newtonsoft(obj);

        [Theory]
        [AutoData]
        public void Serialize_DtoConfigKeyCompletion_System(DtoConfigKeyCompletion obj)
            => TestSerialization_System(obj);

        [Fact]
        public void Serialize_DtoStructure_Newtonsoft()
        {
            var fixture = new Fixture();
            fixture.Customizations.Add(
                new SpecimenFactory<DtoStructure>(
                    () => new DtoStructure
                    {
                        Name = "Foo",
                        Version = 42,
                        Structure = JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement,
                        Variables = new Dictionary<string, object>()
                    }));
            var obj = fixture.Create<DtoStructure>();

            TestSerialization_Newtonsoft(obj);
        }

        [Fact]
        public void Serialize_DtoStructure_System()
        {
            var fixture = new Fixture();
            fixture.Customizations.Add(
                new SpecimenFactory<DtoStructure>(
                    () => new DtoStructure
                    {
                        Name = "Foo",
                        Version = 42,
                        Structure = JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement,
                        Variables = new Dictionary<string, object>()
                    }));
            var obj = fixture.Create<DtoStructure>();

            TestSerialization_System(obj);
        }

        [Theory]
        [AutoData]
        public void Serialize_EnvironmentExport_Newtonsoft(EnvironmentExport obj)
            => TestSerialization_Newtonsoft(obj);

        [Theory]
        [AutoData]
        public void Serialize_EnvironmentExport_System(EnvironmentExport obj)
            => TestSerialization_System(obj);

        [Theory]
        [AutoData]
        public void Serialize_EnvironmentKeyExport_Newtonsoft(EnvironmentKeyExport obj)
            => TestSerialization_Newtonsoft(obj);

        [Theory]
        [AutoData]
        public void Serialize_EnvironmentKeyExport_System(EnvironmentKeyExport obj)
            => TestSerialization_System(obj);

        [Theory]
        [AutoData]
        public void Serialize_ExportDefinition_Newtonsoft(ExportDefinition obj)
            => TestSerialization_Newtonsoft(obj);

        [Theory]
        [AutoData]
        public void Serialize_ExportDefinition_System(ExportDefinition obj)
            => TestSerialization_System(obj);

        [Theory]
        [AutoData]
        public void Serialize_LayerExport_Newtonsoft(LayerExport obj)
            => TestSerialization_Newtonsoft(obj);

        [Theory]
        [AutoData]
        public void Serialize_LayerExport_System(LayerExport obj)
            => TestSerialization_System(obj);
    }
}
