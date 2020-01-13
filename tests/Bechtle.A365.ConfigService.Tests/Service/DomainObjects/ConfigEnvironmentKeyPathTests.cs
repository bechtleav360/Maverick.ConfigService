using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class ConfigEnvironmentKeyPathTests
    {
        [Fact]
        public void CreateNew()
        {
            var path = new ConfigEnvironmentKeyPath();

            Assert.Empty(path.Children);
            Assert.Equal("", path.FullPath);
            Assert.Null(path.Parent);
            Assert.Equal("", path.Path);
        }

        [Fact]
        public void ExceptionOnNullChildren()
        {
            var root = new ConfigEnvironmentKeyPath {Path = "Foo", Children = null};
            var child = new ConfigEnvironmentKeyPath {Path = "Bar", Children = null, Parent = root};

            Assert.NotNull(child.FullPath);
        }

        [Fact]
        public void ExceptionOnNullParent()
        {
            var child = new ConfigEnvironmentKeyPath {Path = "Foo", Parent = null};

            Assert.NotNull(child.FullPath);
        }

        [Fact]
        public void FullPathResolution()
        {
            // basic hierarchy
            var jar = new ConfigEnvironmentKeyPath {Path = "Jar"};
            var baz = new ConfigEnvironmentKeyPath {Path = "Baz"};
            var bar = new ConfigEnvironmentKeyPath
            {
                Path = "Bar",
                Children = {baz}
            };

            var root = new ConfigEnvironmentKeyPath
            {
                Path = "Foo",
                Children = {bar, jar}
            };

            // setup .Parent prop
            foreach (var child in root.Children)
            {
                child.Parent = root;
                foreach (var grandChild in child.Children)
                    grandChild.Parent = child;
            }

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