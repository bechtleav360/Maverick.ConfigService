using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Parsing;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Parsing
{
    public class ConfigValuePartTests
    {
        [Theory]
        [InlineData(ReferenceCommand.None, "")]
        [InlineData(ReferenceCommand.None, null)]
        [InlineData(ReferenceCommand.Using, "some/path/somewhere")]
        [InlineData(ReferenceCommand.Fallback, "42")]
        public void CreateReferencePart(ReferenceCommand command, string value)
            => Assert.NotNull(new ReferencePart(new Dictionary<ReferenceCommand, string> {{command, value}}));

        [Theory]
        [InlineData(ReferenceCommand.None, "")]
        [InlineData(ReferenceCommand.None, null)]
        [InlineData(ReferenceCommand.Using, "some/path/somewhere")]
        [InlineData(ReferenceCommand.Fallback, "42")]
        public void ReadReferencePart(ReferenceCommand command, string value)
        {
            var part = new ReferencePart(new Dictionary<ReferenceCommand, string> {{command, value}});

            Assert.Equal(command, part.Commands.First().Key);
            Assert.Equal(value, part.Commands.First().Value);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("Foo")]
        [InlineData("42")]
        [InlineData("false")]
        public void CreateValuePart(string value) => Assert.NotNull(new ValuePart(value));

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("Foo")]
        [InlineData("42")]
        [InlineData("false")]
        public void ReadValuePart(string value)
        {
            var part = new ValuePart(value);

            Assert.Equal(value, part.Text);
        }

        [Fact]
        public void CreateEmptyReferencePart() => Assert.NotNull(new ReferencePart(new Dictionary<ReferenceCommand, string>()));
    }
}