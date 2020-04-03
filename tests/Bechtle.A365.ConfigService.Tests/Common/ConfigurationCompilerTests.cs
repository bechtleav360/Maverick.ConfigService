using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading;
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

        private void CheckCompilationResult(
            IDictionary<string, string> keys,
            IDictionary<string, string> structKeys,
            IDictionary<string, string> structVars,
            Action<CompilationResult> assertions,
            Func<EnvironmentCompilationInfo, StructureCompilationInfo, CompilationResult> compileFunc = null,
            [CallerMemberName] string testName = null)
        {
            var env = new EnvironmentCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(keys),
                Name = $"{testName}-Environment"
            };

            var structure = new StructureCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string>(structKeys),
                Variables = new ReadOnlyDictionary<string, string>(structVars),
                Name = $"{testName}-Structure"
            };

            var compiled = compileFunc?.Invoke(env, structure) ?? _compiler.Compile(env, structure, _parser);

            assertions(compiled);
        }

        /// <summary>
        ///     resolve a reference to a complex result (object)
        /// </summary>
        [Fact]
        public void CompileExpandObject()
            => CheckCompilationResult(
                new Dictionary<string, string>
                {
                    {"A/A", "A"},
                    {"A/B", "B"},
                    {"A/C", "C"},
                    {"A/D", "D"},
                    {"A/E", "E"}
                },
                new Dictionary<string, string> {{"A", "{{A/*}}"}},
                new Dictionary<string, string>(),
                result =>
                {
                    var compiled = result.CompiledConfiguration;

                    Assert.NotNull(compiled);
                    Assert.Equal("A", compiled["A/A"]);
                    Assert.Equal("B", compiled["A/B"]);
                    Assert.Equal("C", compiled["A/C"]);
                    Assert.Equal("D", compiled["A/D"]);
                    Assert.Equal("E", compiled["A/E"]);
                });

        /// <summary>
        ///     compile a reference with a malformed syntax.
        ///     this might actually be wanted behaviour, and expanded on in the future
        /// </summary>
        [Fact]
        public void CompileExpandObjectFilterKeys()
            => CheckCompilationResult(
                new Dictionary<string, string>
                {
                    {"A/A", "A"},
                    {"A/B", "B"},
                    {"A/C", "C"},
                    {"A/D", "D"},
                    {"A/E", "E"}
                },
                new Dictionary<string, string> {{"A", "{{A*}}"}},
                new Dictionary<string, string>(),
                result =>
                {
                    var compiled = result.CompiledConfiguration;

                    Assert.NotNull(compiled);
                    // actually not sure if this is what we want...
                    // might be handy if one wants to include all keys that match 'Name*' or something like that
                    Assert.Equal("A", compiled["A/A"]);
                    Assert.Equal("B", compiled["A/B"]);
                    Assert.Equal("C", compiled["A/C"]);
                    Assert.Equal("D", compiled["A/D"]);
                    Assert.Equal("E", compiled["A/E"]);
                });

        /// <summary>
        ///     handle a reference to a complex result with malformed syntax (missing '/*')
        /// </summary>
        [Fact]
        public void CompileExpandObjectWrongSyntax()
            => CheckCompilationResult(
                new Dictionary<string, string>
                {
                    {"A/A", "A"},
                    {"A/B", "B"},
                    {"A/C", "C"},
                    {"A/D", "D"},
                    {"A/E", "E"}
                },
                new Dictionary<string, string> {{"A", "{{A}}"}},
                new Dictionary<string, string>(),
                result =>
                {
                    var compiled = result.CompiledConfiguration;

                    Assert.NotNull(compiled);
                    Assert.Single(compiled);
                    Assert.Equal(string.Empty, compiled["A"]);
                });

        /// <summary>
        ///     handle (seemingly-)infinite recursion gracefully without throwing up
        /// </summary>
        [Fact]
        public void CompileInfiniteRecursion()
            => CheckCompilationResult(
                new Dictionary<string, string>
                {
                    {"A", "{{B}}"},
                    {"B", "{{A}}"}
                },
                new Dictionary<string, string> {{"A", "{{A}}"}},
                new Dictionary<string, string>(),
                result =>
                {
                    var compiled = result.CompiledConfiguration;

                    Assert.NotNull(compiled);
                    Assert.NotEmpty(compiled);
                    Assert.Equal("", compiled["A"]);
                },
                (env, structure) =>
                {
                    CompilationResult r = null;

                    var thread = new Thread(() => { r = _compiler.Compile(env, structure, _parser); });

                    thread.Start();

                    // wait for the compilation to finish, but it should finish within a couple seconds
                    // otherwise it's probably caught in an endless loop
                    thread.Join(TimeSpan.FromSeconds(5));

                    // r will be set if compilation was successful
                    Assert.NotNull(r);

                    return r;
                });

        /// <summary>
        ///     resolve a reference that points to another reference and so on
        /// </summary>
        [Fact]
        public void CompileRecursiveReference()
            => CheckCompilationResult(
                new Dictionary<string, string>
                {
                    {"A", "{{B}}"},
                    {"B", "{{C}}"},
                    {"C", "{{D}}"},
                    {"D", "{{E}}"},
                    {"E", "ResolvedValue"}
                },
                new Dictionary<string, string>
                {
                    {"A", "{{A}}"},
                    {"B", "{{B}}"},
                    {"C", "{{C}}"},
                    {"D", "{{D}}"},
                    {"E", "{{E}}"}
                },
                new Dictionary<string, string>(),
                result =>
                {
                    var compiled = result.CompiledConfiguration;

                    Assert.NotNull(compiled);
                    Assert.Equal(5, compiled.Count);
                    Assert.Equal("ResolvedValue", compiled["A"]);
                    Assert.Equal("ResolvedValue", compiled["B"]);
                    Assert.Equal("ResolvedValue", compiled["C"]);
                    Assert.Equal("ResolvedValue", compiled["D"]);
                    Assert.Equal("ResolvedValue", compiled["E"]);
                });

        /// <summary>
        ///     resolve a simple reference with a more involved name
        /// </summary>
        [Fact]
        public void CompileReferenceWithPath()
            => CheckCompilationResult(
                new Dictionary<string, string> {{"Key/With/Path", "ResolvedValue"}},
                new Dictionary<string, string> {{"A", "{{Key/With/Path}}"}},
                new Dictionary<string, string>(),
                result =>
                {
                    var compiled = result.CompiledConfiguration;

                    Assert.NotNull(compiled);
                    Assert.Equal(1, compiled.Count);
                    Assert.Equal("ResolvedValue", compiled["A"]);
                });

        /// <summary>
        ///     resolve a simple reference to env
        /// </summary>
        [Fact]
        public void CompileSimpleReference()
            => CheckCompilationResult(
                new Dictionary<string, string> {{"C", "CV"}},
                new Dictionary<string, string>
                {
                    {"A", "AV"},
                    {"B", "BV"},
                    {"C", "{{C}}"}
                },
                new Dictionary<string, string>(),
                result =>
                {
                    var compiled = result.CompiledConfiguration;

                    Assert.NotNull(compiled);

                    Assert.Equal(3, compiled.Count);
                    Assert.Equal("AV", compiled["A"]);
                    Assert.Equal("BV", compiled["B"]);
                    Assert.Equal("CV", compiled["C"]);
                });

        /// <summary>
        ///     compile a structure with a reference to an Environment-Key that references the current Structure-Variables
        /// </summary>
        [Fact]
        public void CompileVariableRefFromEnvironment()
            => CheckCompilationResult(
                new Dictionary<string, string> {{"E", "{{$struct/S}}"}},
                new Dictionary<string, string> {{"S", "{{E}}"}},
                new Dictionary<string, string> {{"S", "SV"}},
                result =>
                {
                    var compiled = result.CompiledConfiguration;

                    Assert.NotNull(compiled);
                    Assert.NotEmpty(compiled);
                    Assert.Equal("SV", compiled["S"]);
                });

        /// <summary>
        ///     compile a structure with a direct-reference to its own variables
        /// </summary>
        [Fact]
        public void CompileVariableRefFromStructure()
            => CheckCompilationResult(
                new Dictionary<string, string> {{"", ""}},
                new Dictionary<string, string> {{"S", "{{$struct/S}}"}},
                new Dictionary<string, string> {{"S", "SV"}},
                result =>
                {
                    var compiled = result.CompiledConfiguration;

                    Assert.NotNull(compiled);
                    Assert.NotEmpty(compiled);
                    Assert.Equal("SV", compiled["S"]);
                });

        /// <summary>
        ///     references to null will be replaced empty string for safety
        /// </summary>
        [Fact]
        public void DirectReferenceErasesNull()
            => CheckCompilationResult(
                new Dictionary<string, string>
                {
                    {"C", null},
                    {"D", ""}
                },
                new Dictionary<string, string>
                {
                    {"A", "{{C}}"},
                    {"B", "{{D}}"}
                },
                new Dictionary<string, string>(),
                result =>
                {
                    var compiled = result.CompiledConfiguration;

                    Assert.NotNull(compiled);

                    Assert.Equal(2, compiled.Count);
                    Assert.Equal(string.Empty, compiled["A"]);
                    Assert.Equal(string.Empty, compiled["B"]);
                });

        [Fact]
        public void LongCyclicReference()
            => CheckCompilationResult(
                new Dictionary<string, string>
                {
                    {"A", "{{B}}"},
                    {"B", "{{C}}"},
                    {"C", "{{D}}"},
                    {"D", "{{E}}"},
                    {"E", "{{F}}"},
                    {"F", "{{G}}"},
                    {"G", "{{H}}"},
                    {"H", "{{A}}"}
                },
                new Dictionary<string, string> {{"A", "{{A}}"}},
                new Dictionary<string, string>(),
                result => { Assert.Equal("", result.CompiledConfiguration["A"]); });

        /// <summary>
        ///     don't fail when there is nothing to do
        /// </summary>
        [Fact]
        public void NoCompilationNeeded()
            => CheckCompilationResult(
                new Dictionary<string, string>(),
                new Dictionary<string, string>(),
                new Dictionary<string, string>(),
                result =>
                {
                    var compiled = result.CompiledConfiguration;

                    Assert.NotNull(compiled);
                    Assert.Empty(compiled);
                });

        /// <summary>
        ///     compile environment-values that have null as value, and preserve them in the output
        /// </summary>
        [Fact]
        public void SectionCompilationPreservesNull()
            => CheckCompilationResult(
                new Dictionary<string, string>
                {
                    {"SectionWithNull/First", null},
                    {"SectionWithNull/Second", null},
                    {"D", ""}
                },
                new Dictionary<string, string>
                {
                    {"A", "{{SectionWithNull/*}}"},
                    {"B", "{{D}}"}
                },
                new Dictionary<string, string>(),
                result =>
                {
                    var compiled = result.CompiledConfiguration;

                    Assert.NotNull(compiled);

                    Assert.Equal(3, compiled.Count);
                    Assert.Null(compiled["A/First"]);
                    Assert.Null(compiled["A/Second"]);
                    Assert.Equal(string.Empty, compiled["B"]);
                });
    }
}