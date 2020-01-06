using Bechtle.A365.ConfigService.Common;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common
{
    public class ConfigKeyActionTests
    {
        [Fact]
        public void CreateDeleteActionViaConstructor()
        {
            var action = new ConfigKeyAction(ConfigKeyActionType.Delete, "Foo", "Bar", "Baz", "Que?");

            Assert.Equal(ConfigKeyActionType.Delete, action.Type);
            Assert.Equal("Foo", action.Key);
            Assert.Equal("Bar", action.Value);
            Assert.Equal("Baz", action.Description);
            Assert.Equal("Que?", action.ValueType);
        }

        [Fact]
        public void CreateDeleteActionViaShortcut()
        {
            var action = ConfigKeyAction.Delete("Foo");

            Assert.Equal(ConfigKeyActionType.Delete, action.Type);
            Assert.Equal("Foo", action.Key);
        }

        [Fact]
        public void CreateSetActionViaConstructor()
        {
            var action = new ConfigKeyAction(ConfigKeyActionType.Set, "Foo", "Bar", "Baz", "Que?");

            Assert.Equal(ConfigKeyActionType.Set, action.Type);
            Assert.Equal("Foo", action.Key);
            Assert.Equal("Bar", action.Value);
            Assert.Equal("Baz", action.Description);
            Assert.Equal("Que?", action.ValueType);
        }

        [Fact]
        public void CreateSetActionViaShortcut()
        {
            var action = ConfigKeyAction.Set("Foo", "Bar");

            Assert.NotNull(action);
            Assert.Equal(ConfigKeyActionType.Set, action.Type);
            Assert.Equal("Foo", action.Key);
            Assert.Equal("Bar", action.Value);
        }

        [Fact]
        public void CreateSetActionViaShortcutFull()
        {
            var action = ConfigKeyAction.Set("Foo", "Bar", "Baz", "Que?");

            Assert.NotNull(action);
            Assert.Equal(ConfigKeyActionType.Set, action.Type);
            Assert.Equal("Foo", action.Key);
            Assert.Equal("Bar", action.Value);
            Assert.Equal("Baz", action.Description);
            Assert.Equal("Que?", action.ValueType);
        }
    }
}