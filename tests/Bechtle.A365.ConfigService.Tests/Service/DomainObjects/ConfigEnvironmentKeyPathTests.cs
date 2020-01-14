using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class ConfigEnvironmentKeyPathTests
    {
        [Fact]
        public void CreateNew()
        {
            var path = new ConfigEnvironmentKeyPath("");

            Assert.Empty(path.Children);
            Assert.Equal("", path.FullPath);
            Assert.Null(path.Parent);
            Assert.Equal("", path.Path);
        }

        [Fact]
        public void ExceptionOnNullChildren()
        {
            var root = new ConfigEnvironmentKeyPath("Foo");
            var child = new ConfigEnvironmentKeyPath("Bar", root);

            Assert.NotNull(child.FullPath);
        }

        [Fact]
        public void ExceptionOnNullParent()
        {
            var child = new ConfigEnvironmentKeyPath("Foo");

            Assert.NotNull(child.FullPath);
        }

        [Fact]
        public void FullPathResolution()
        {
            // setup hierarchy
            var root = new ConfigEnvironmentKeyPath("Foo");
            var bar = new ConfigEnvironmentKeyPath("Bar", root);
            var baz = new ConfigEnvironmentKeyPath("Baz", bar);
            var jar = new ConfigEnvironmentKeyPath("Jar", root);

            root.Children.AddRange(new[] {bar, jar});
            bar.Children.Add(baz);

            // actual tests
            // no trailing / because no children
            Assert.Equal("Foo/Jar", jar.FullPath);
            Assert.Equal("Foo/Bar/Baz", baz.FullPath);

            // trailing / because of children
            Assert.Equal("Foo/Bar/", bar.FullPath);
            Assert.Equal("Foo/", root.FullPath);
        }
    }
}