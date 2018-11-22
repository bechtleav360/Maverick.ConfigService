﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Parsing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests
{
    // using ReadOnlyDictionaries to ensure the compiler can't write to the given collections
    public class ConfigurationCompilerTests
    {
        private static IConfigurationCompiler Compiler => new ConfigurationCompiler(new LoggerFactory().AddConsole(LogLevel.Warning)
                                                                                                       .CreateLogger<ConfigurationCompiler>());

        private static IConfigurationParser Parser => new AntlrConfigurationParser();

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

            var compiled = Compiler.Compile(env, structure, Parser);

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

            var compiled = Compiler.Compile(env, structure, Parser);

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

            var compiled = Compiler.Compile(env, structure, Parser);

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

            var compiled = Compiler.Compile(env, structure, Parser);

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

            var compiled = Compiler.Compile(env, structure, Parser);

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

            var compiled = Compiler.Compile(env, structure, Parser);

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

            var compiled = Compiler.Compile(env, structure, Parser);

            Assert.NotNull(compiled);

            Assert.Equal(3, compiled.Count);
            Assert.Equal("AV", compiled["A"]);
            Assert.Equal("BV", compiled["B"]);
            Assert.Equal("CV", compiled["C"]);
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

            var compiled = Compiler.Compile(env, structure, Parser);

            Assert.NotNull(compiled);
            Assert.Empty(compiled);
        }
    }
}