using Newtonsoft.Json;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Bechtle.A365.ConfigService.Tests.Utilities
{
    public abstract class SerializationTestBase
    {
        protected static void TestDeserialization_Newtonsoft<T>(T obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            var deserialized = JsonConvert.DeserializeObject<T>(json);
            Assert.Equal(obj, deserialized);
        }

        protected static void TestDeserialization_System<T>(T obj)
        {
            string json = JsonSerializer.Serialize(obj);
            var deserialized = JsonSerializer.Deserialize<T>(json);
            Assert.Equal(obj, deserialized);
        }

        protected static void TestSerialization_Newtonsoft<T>(T obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            Assert.NotEmpty(json);
        }

        protected static void TestSerialization_System<T>(T obj)
        {
            string json = JsonSerializer.Serialize(obj);
            Assert.NotEmpty(json);
        }
    }
}
