using System.Collections.Generic;
using System.Collections.ObjectModel;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common
{
    // using ReadOnlyDictionaries to ensure the compiler can't write to the given collections
    public class ConfigurationCompilerTests
    {
        public ConfigurationCompilerTests()
        {
            _compiler = new ConfigurationCompiler(
                new ServiceCollection().AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning))
                                       .BuildServiceProvider()
                                       .GetRequiredService<ILogger<ConfigurationCompiler>>());

            _parser = new AntlrConfigurationParser();
        }

        private readonly IConfigurationCompiler _compiler;
        private readonly IConfigurationParser _parser;

        /// <summary>
        ///     resolve a reference to a complex result (object)
        /// </summary>
        [Fact]
        public void CompileExpandObject()
        {
            var env = new EnvironmentCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"A/A", "A"},
                    {"A/B", "B"},
                    {"A/C", "C"},
                    {"A/D", "D"},
                    {"A/E", "E"}
                })
            };

            var structure = new StructureCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"A", "{{A/*}}"}
                }),
                Variables = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())
            };

            var compiled = _compiler.Compile(env, structure, _parser).CompiledConfiguration;

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
            var env = new EnvironmentCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"A/A", "A"},
                    {"A/B", "B"},
                    {"A/C", "C"},
                    {"A/D", "D"},
                    {"A/E", "E"}
                })
            };

            var structure = new StructureCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"A", "{{A*}}"}
                }),
                Variables = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())
            };

            var compiled = _compiler.Compile(env, structure, _parser).CompiledConfiguration;

            Assert.NotNull(compiled);
            // actually not sure if this is what we want...
            // might be handy if one wants to include all keys that match 'Name*' or something like that
            Assert.Equal("A", compiled["A/A"]);
            Assert.Equal("B", compiled["A/B"]);
            Assert.Equal("C", compiled["A/C"]);
            Assert.Equal("D", compiled["A/D"]);
            Assert.Equal("E", compiled["A/E"]);
        }

        /// <summary>
        ///     handle a reference to a complex result with malformed syntax (missing '/*')
        /// </summary>
        [Fact]
        public void CompileExpandObjectWrongSyntax()
        {
            var env = new EnvironmentCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"A/A", "A"},
                    {"A/B", "B"},
                    {"A/C", "C"},
                    {"A/D", "D"},
                    {"A/E", "E"}
                })
            };

            var structure = new StructureCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"A", "{{A}}"}
                }),
                Variables = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())
            };

            var compiled = _compiler.Compile(env, structure, _parser).CompiledConfiguration;

            Assert.NotNull(compiled);
            Assert.Equal(string.Empty, compiled["A"]);
        }

        /// <summary>
        ///     handle (seemingly-)infinite recursion gracefully without throwing up
        /// </summary>
        [Fact]
        public void CompileInfiniteRecursion()
        {
            var env = new EnvironmentCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"A", "{{B}}"},
                    {"B", "{{A}}"}
                })
            };

            var structure = new StructureCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"A", "{{A}}"}
                }),
                Variables = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())
            };

            var compiled = _compiler.Compile(env, structure, _parser).CompiledConfiguration;

            Assert.NotNull(compiled);
            Assert.NotEmpty(compiled);
            Assert.Equal("", compiled["A"]);
        }

        /// <summary>
        ///     resolve a reference that points to another reference and so on
        /// </summary>
        [Fact]
        public void CompileRecursiveReference()
        {
            var env = new EnvironmentCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"A", "{{B}}"},
                    {"B", "{{C}}"},
                    {"C", "{{D}}"},
                    {"D", "{{E}}"},
                    {"E", "ResolvedValue"}
                })
            };

            var structure = new StructureCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"A", "{{A}}"},
                    {"B", "{{B}}"},
                    {"C", "{{C}}"},
                    {"D", "{{D}}"},
                    {"E", "{{E}}"}
                }),
                Variables = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())
            };

            var compiled = _compiler.Compile(env, structure, _parser).CompiledConfiguration;

            Assert.NotNull(compiled);
            Assert.Equal(5, compiled.Count);
            Assert.Equal("ResolvedValue", compiled["A"]);
            Assert.Equal("ResolvedValue", compiled["B"]);
            Assert.Equal("ResolvedValue", compiled["C"]);
            Assert.Equal("ResolvedValue", compiled["D"]);
            Assert.Equal("ResolvedValue", compiled["E"]);
        }

        /// <summary>
        ///     resolve a simple reference with a more involved name
        /// </summary>
        [Fact]
        public void CompileReferenceWithPath()
        {
            var env = new EnvironmentCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"Key/With/Path", "ResolvedValue"}
                })
            };

            var structure = new StructureCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"A", "{{Key/With/Path}}"}
                }),
                Variables = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())
            };

            var compiled = _compiler.Compile(env, structure, _parser).CompiledConfiguration;

            Assert.NotNull(compiled);
            Assert.Equal(1, compiled.Count);
            Assert.Equal("ResolvedValue", compiled["A"]);
        }

        /// <summary>
        ///     resolve a simple reference to env
        /// </summary>
        [Fact]
        public void CompileSimpleReference()
        {
            var env = new EnvironmentCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"C", "CV"}
                })
            };

            var structure = new StructureCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"A", "AV"},
                    {"B", "BV"},
                    {"C", "{{C}}"}
                }),
                Variables = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())
            };

            var compiled = _compiler.Compile(env, structure, _parser).CompiledConfiguration;

            Assert.NotNull(compiled);

            Assert.Equal(3, compiled.Count);
            Assert.Equal("AV", compiled["A"]);
            Assert.Equal("BV", compiled["B"]);
            Assert.Equal("CV", compiled["C"]);
        }

        /// <summary>
        ///     references to null will be replaced empty string for safety
        /// </summary>
        [Fact]
        public void DirectReferenceErasesNull()
        {
            var env = new EnvironmentCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"C", null},
                    {"D", ""}
                })
            };

            var structure = new StructureCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"A", "{{C}}"},
                    {"B", "{{D}}"}
                }),
                Variables = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())
            };

            var compiled = _compiler.Compile(env, structure, _parser).CompiledConfiguration;

            Assert.NotNull(compiled);

            Assert.Equal(2, compiled.Count);
            Assert.Equal(string.Empty, compiled["A"]);
            Assert.Equal(string.Empty, compiled["B"]);
        }

        /// <summary>
        ///     don't fail when there is nothing to do
        /// </summary>
        [Fact]
        public void NoCompilationNeeded()
        {
            var env = new EnvironmentCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())
            };

            var structure = new StructureCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()),
                Variables = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())
            };

            var compiled = _compiler.Compile(env, structure, _parser).CompiledConfiguration;

            Assert.NotNull(compiled);
            Assert.Empty(compiled);
        }

        /// <summary>
        ///     compile environment-values that have null as value, and preserve them in the output
        /// </summary>
        [Fact]
        public void SectionCompilationPreservesNull()
        {
            var env = new EnvironmentCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"SectionWithNull/First", null},
                    {"SectionWithNull/Second", null},
                    {"D", ""}
                })
            };

            var structure = new StructureCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"A", "{{SectionWithNull/*}}"},
                    {"B", "{{D}}"}
                }),
                Variables = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())
            };

            var compiled = _compiler.Compile(env, structure, _parser).CompiledConfiguration;

            Assert.NotNull(compiled);

            Assert.Equal(3, compiled.Count);
            Assert.Null(compiled["A/First"]);
            Assert.Null(compiled["A/Second"]);
            Assert.Equal(string.Empty, compiled["B"]);
        }
    }
}