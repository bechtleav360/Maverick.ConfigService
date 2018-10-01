using System.Collections.Generic;
using System.Collections.ObjectModel;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Projection;
using Bechtle.A365.ConfigService.Projection.Compilation;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests
{
    // using ReadOnlyDictionaries to ensure the compiler can't write to the given collections
    public class ConfigurationCompilerTests
    {
        private IConfigurationCompiler Compiler => new ConfigurationCompiler(new LoggerFactory().AddConsole(LogLevel.Warning)
                                                                                                .CreateLogger<ConfigurationCompiler>());

        private IConfigurationParser Parser => new ConfigurationParser();

        /// <summary>
        ///     don't fail when there is nothing to do
        /// </summary>
        [Fact]
        public void NoCompilationNeeded()
        {
            IDictionary<string, string> env = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
            IDictionary<string, string> structure = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

            var compiled = Compiler.Compile(env, structure, Parser)
                                   .RunSync();

            Assert.NotNull(compiled);
            Assert.Empty(compiled);
        }

        /// <summary>
        ///     resolve a simple reference to env
        /// </summary>
        [Fact]
        public void CompileSimpleReference()
        {
            IDictionary<string, string> env = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                {"C", "CV"},
            });

            IDictionary<string, string> structure = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                {"A", "AV"},
                {"B", "BV"},
                {"C", "{{C}}"},
            });

            var compiled = Compiler.Compile(env, structure, Parser)
                                   .RunSync();

            Assert.NotNull(compiled);

            Assert.Equal(3, compiled.Count);
            Assert.Equal("AV", compiled["A"]);
            Assert.Equal("BV", compiled["B"]);
            Assert.Equal("CV", compiled["C"]);
        }

        /// <summary>
        ///     resolve a simple reference with a more involved name
        /// </summary>
        [Fact]
        public void CompileReferenceWithPath()
        {
            IDictionary<string, string> env = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                {"Key/With/Path", "ResolvedValue"}
            });

            IDictionary<string, string> structure = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                {"A", "{{Key/With/Path}}"}
            });

            var compiled = Compiler.Compile(env, structure, Parser)
                                   .RunSync();

            Assert.NotNull(compiled);
            Assert.Equal(1, compiled.Count);
            Assert.Equal("ResolvedValue", compiled["A"]);
        }

        /// <summary>
        ///     resolve a reference that points to another reference and so on
        /// </summary>
        [Fact]
        public void CompileRecursiveReference()
        {
            IDictionary<string, string> env = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                {"A", "{{B}}"},
                {"B", "{{C}}"},
                {"C", "{{D}}"},
                {"D", "{{E}}"},
                {"E", "ResolvedValue"}
            });

            IDictionary<string, string> structure = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                {"A", "{{A}}"},
                {"B", "{{B}}"},
                {"C", "{{C}}"},
                {"D", "{{D}}"},
                {"E", "{{E}}"}
            });

            var compiled = Compiler.Compile(env, structure, Parser)
                                   .RunSync();

            Assert.NotNull(compiled);
            Assert.Equal(5, compiled.Count);
            Assert.Equal("ResolvedValue", compiled["A"]);
            Assert.Equal("ResolvedValue", compiled["B"]);
            Assert.Equal("ResolvedValue", compiled["C"]);
            Assert.Equal("ResolvedValue", compiled["D"]);
            Assert.Equal("ResolvedValue", compiled["E"]);
        }

        /// <summary>
        ///     handle (seemingly-)infinite recursion gracefully without throwing up
        /// </summary>
        [Fact]
        public void CompileInfiniteRecursion()
        {
            IDictionary<string, string> env = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                {"A", "{{B}}"},
                {"B", "{{A}}"}
            });

            IDictionary<string, string> structure = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                {"A", "{{A}}"}
            });

            var compiled = Compiler.Compile(env, structure, Parser)
                                   .RunSync();

            Assert.NotNull(compiled);
            Assert.Equal("", compiled["A"]);
        }

        /// <summary>
        ///     resolve a reference to a complex result (object)
        /// </summary>
        [Fact]
        public void CompileExpandObject()
        {
            IDictionary<string, string> env = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                {"A/A", "A"},
                {"A/B", "B"},
                {"A/C", "C"},
                {"A/D", "D"},
                {"A/E", "E"},
            });

            IDictionary<string, string> structure = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                {"A", "{{A/*}}"}
            });

            var compiled = Compiler.Compile(env, structure, Parser)
                                   .RunSync();

            Assert.NotNull(compiled);
            Assert.Equal("A", compiled["A/A"]);
            Assert.Equal("B", compiled["A/B"]);
            Assert.Equal("C", compiled["A/C"]);
            Assert.Equal("D", compiled["A/D"]);
            Assert.Equal("E", compiled["A/E"]);
        }

        /// <summary>
        ///     compile a reference with a malformed syntax.
        ///     this might actually be wanted behaviour, and expanded on in the future
        /// </summary>
        [Fact]
        public void CompileExpandObjectFilterKeys()
        {
            IDictionary<string, string> env = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                {"A/A", "A"},
                {"A/B", "B"},
                {"A/C", "C"},
                {"A/D", "D"},
                {"A/E", "E"},
            });

            IDictionary<string, string> structure = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                {"A", "{{A*}}"}
            });

            var compiled = Compiler.Compile(env, structure, Parser)
                                   .RunSync();

            Assert.NotNull(compiled);
            // actually not sure if this is what we want...
            // might be handy if one wants to include all keys that match 'Name*' or something like that
            Assert.Equal("A", compiled["AA"]);
            Assert.Equal("B", compiled["AB"]);
            Assert.Equal("C", compiled["AC"]);
            Assert.Equal("D", compiled["AD"]);
            Assert.Equal("E", compiled["AE"]);
        }

        /// <summary>
        ///     handle a reference to a complex result with malformed syntax (missing '/*')
        /// </summary>
        [Fact]
        public void CompileExpandObjectWrongSyntax()
        {
            IDictionary<string, string> env = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                {"A/A", "A"},
                {"A/B", "B"},
                {"A/C", "C"},
                {"A/D", "D"},
                {"A/E", "E"},
            });

            IDictionary<string, string> structure = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                {"A", "{{A}}"}
            });

            var compiled = Compiler.Compile(env, structure, Parser)
                                   .RunSync();

            Assert.NotNull(compiled);
            Assert.Equal(string.Empty, compiled["A"]);
        }
    }
}