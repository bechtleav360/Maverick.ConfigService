﻿using System;
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
        /// <summary>
        ///     resolve a reference to a complex result (object)
        /// </summary>
        [Fact]
        public void CompileExpandObject()
            => CheckCompilationResult(
                new Dictionary<string, string?>
                {
                    { "A/A", "A" },
                    { "A/B", "B" },
                    { "A/C", "C" },
                    { "A/D", "D" },
                    { "A/E", "E" }
                },
                new Dictionary<string, string?> { { "A", "{{A/*}}" } },
                new Dictionary<string, string?>(),
                new Dictionary<string, string?>(),
                result =>
                {
                    IDictionary<string, string?> compiled = result.CompiledConfiguration;

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
                new Dictionary<string, string?>
                {
                    { "A/A", "A" },
                    { "A/B", "B" },
                    { "A/C", "C" },
                    { "A/D", "D" },
                    { "A/E", "E" }
                },
                new Dictionary<string, string?> { { "A", "{{A*}}" } },
                new Dictionary<string, string?>(),
                new Dictionary<string, string?>(),
                result =>
                {
                    IDictionary<string, string?> compiled = result.CompiledConfiguration;

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
                new Dictionary<string, string?>
                {
                    { "A/A", "A" },
                    { "A/B", "B" },
                    { "A/C", "C" },
                    { "A/D", "D" },
                    { "A/E", "E" }
                },
                new Dictionary<string, string?> { { "A", "{{A}}" } },
                new Dictionary<string, string?>(),
                new Dictionary<string, string?>(),
                result =>
                {
                    IDictionary<string, string?> compiled = result.CompiledConfiguration;

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
                new Dictionary<string, string?>
                {
                    { "A", "{{B}}" },
                    { "B", "{{A}}" }
                },
                new Dictionary<string, string?> { { "A", "{{A}}" } },
                new Dictionary<string, string?>(),
                new Dictionary<string, string?>(),
                result =>
                {
                    IDictionary<string, string?> compiled = result.CompiledConfiguration;

                    Assert.NotNull(compiled);
                    Assert.NotEmpty(compiled);
                    Assert.Equal("", compiled["A"]);
                },
                (compiler, parser, env, structure) =>
                {
                    CompilationResult? r = null;

                    var thread = new Thread(() => { r = compiler.Compile(env, structure, parser); });

                    thread.Start();

                    // wait for the compilation to finish, but it should finish within a couple seconds
                    // otherwise it's probably caught in an endless loop
                    thread.Join(TimeSpan.FromSeconds(5));

                    // r will be set if compilation was successful
                    Assert.NotNull(r);

                    return r!;
                });

        /// <summary>
        ///     resolve a reference that points to another reference and so on
        /// </summary>
        [Fact]
        public void CompileRecursiveReference()
            => CheckCompilationResult(
                new Dictionary<string, string?>
                {
                    { "A", "{{B}}" },
                    { "B", "{{C}}" },
                    { "C", "{{D}}" },
                    { "D", "{{E}}" },
                    { "E", "ResolvedValue" }
                },
                new Dictionary<string, string?>
                {
                    { "A", "{{A}}" },
                    { "B", "{{B}}" },
                    { "C", "{{C}}" },
                    { "D", "{{D}}" },
                    { "E", "{{E}}" }
                },
                new Dictionary<string, string?>(),
                new Dictionary<string, string?>(),
                result =>
                {
                    IDictionary<string, string?> compiled = result.CompiledConfiguration;

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
                new Dictionary<string, string?> { { "Key/With/Path", "ResolvedValue" } },
                new Dictionary<string, string?> { { "A", "{{Key/With/Path}}" } },
                new Dictionary<string, string?>(),
                new Dictionary<string, string?>(),
                result =>
                {
                    IDictionary<string, string?> compiled = result.CompiledConfiguration;

                    Assert.NotNull(compiled);
                    Assert.Equal(1, compiled.Count);
                    Assert.Equal("ResolvedValue", compiled["A"]);
                });

        /// <summary>
        ///     check if $secret paths will be taken from the configured SecretStore
        /// </summary>
        [Fact]
        public void CompileSecrets() => CheckCompilationResult(
            new Dictionary<string, string?>(),
            new Dictionary<string, string?> { { "Foo", "{{$secret/Bar}}" } },
            new Dictionary<string, string?>(),
            new Dictionary<string, string?> { { "Bar", "Secret" } },
            result =>
            {
                IDictionary<string, string?> compiled = result.CompiledConfiguration;

                Assert.Equal("Secret", compiled["Foo"]);
            });

        /// <summary>
        ///     resolve a simple reference to env
        /// </summary>
        [Fact]
        public void CompileSimpleReference()
            => CheckCompilationResult(
                new Dictionary<string, string?> { { "C", "CV" } },
                new Dictionary<string, string?>
                {
                    { "A", "AV" },
                    { "B", "BV" },
                    { "C", "{{C}}" }
                },
                new Dictionary<string, string?>(),
                new Dictionary<string, string?>(),
                result =>
                {
                    IDictionary<string, string?> compiled = result.CompiledConfiguration;

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
                new Dictionary<string, string?> { { "E", "{{$struct/S}}" } },
                new Dictionary<string, string?> { { "S", "{{E}}" } },
                new Dictionary<string, string?> { { "S", "SV" } },
                new Dictionary<string, string?>(),
                result =>
                {
                    IDictionary<string, string?> compiled = result.CompiledConfiguration;

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
                new Dictionary<string, string?> { { "", "" } },
                new Dictionary<string, string?> { { "S", "{{$struct/S}}" } },
                new Dictionary<string, string?> { { "S", "SV" } },
                new Dictionary<string, string?>(),
                result =>
                {
                    IDictionary<string, string?> compiled = result.CompiledConfiguration;

                    Assert.NotNull(compiled);
                    Assert.NotEmpty(compiled);
                    Assert.Equal("SV", compiled["S"]);
                });

        /// <summary>
        ///     check if $this paths point to the correct parent-path at top-level
        /// </summary>
        [Fact]
        public void DereferenceThis()
            => CheckCompilationResult(
                new Dictionary<string, string?>
                {
                    { "A", "{{$this/B}}" },
                    { "B", "true" }
                },
                new Dictionary<string, string?> { { "A", "{{A}}" } },
                new Dictionary<string, string?>(),
                new Dictionary<string, string?>(),
                result => Assert.Equal("true", result.CompiledConfiguration["A"]));

        /// <summary>
        ///     check if $this paths point to the correct parent-path at sub-levels
        /// </summary>
        [Fact]
        public void DereferenceThisIndirect()
            => CheckCompilationResult(
                new Dictionary<string, string?>
                {
                    { "A/B/C", "{{$this/D}}" },
                    { "A/B/D", "true" },
                    { "A/D", "false" },
                    { "D", "false" }
                },
                new Dictionary<string, string?> { { "A", "{{A/B/C}}" } },
                new Dictionary<string, string?>(),
                new Dictionary<string, string?>(),
                result => Assert.Equal("true", result.CompiledConfiguration["A"]));

        /// <summary>
        ///     references to null will be replaced empty string for safety
        /// </summary>
        [Fact]
        public void DirectReferenceErasesNull()
            => CheckCompilationResult(
                new Dictionary<string, string?>
                {
                    { "C", null },
                    { "D", "" }
                },
                new Dictionary<string, string?>
                {
                    { "A", "{{C}}" },
                    { "B", "{{D}}" }
                },
                new Dictionary<string, string?>(),
                new Dictionary<string, string?>(),
                result =>
                {
                    IDictionary<string, string?> compiled = result.CompiledConfiguration;

                    Assert.NotNull(compiled);

                    Assert.Equal(2, compiled.Count);
                    Assert.Equal(string.Empty, compiled["A"]);
                    Assert.Equal(string.Empty, compiled["B"]);
                });

        [Fact]
        public void IntermediateReferenceSections()
            => CheckCompilationResult(
                new Dictionary<string, string?>
                {
                    { "NamedEndpoints/IdentityService-External", "{{$this/IdentityService/*}}" },
                    { "NamedEndpoints/IdentityService/Address", "identity.foo.bar" },
                    { "NamedEndpoints/IdentityService/Name", "identityService" },
                    { "NamedEndpoints/IdentityService/Port", "443" },
                    { "NamedEndpoints/IdentityService/Protocol", "https" },
                    { "NamedEndpoints/IdentityService/RootPath", "" },
                    { "NamedEndpoints/IdentityService/Uri", "{{$this/Protocol}}://{{$this/Address}}:{{$this/Port}}{{$this/RootPath}}" }
                },
                new Dictionary<string, string?> { { "IdentityService", "{{NamedEndpoints/IdentityService-External/Uri}}" } },
                new Dictionary<string, string?>(),
                new Dictionary<string, string?>(),
                result => Assert.Equal("https://identity.foo.bar:443", result.CompiledConfiguration["IdentityService"]));

        [Fact]
        public void LongCyclicReference()
            => CheckCompilationResult(
                new Dictionary<string, string?>
                {
                    { "A", "{{B}}" },
                    { "B", "{{C}}" },
                    { "C", "{{D}}" },
                    { "D", "{{E}}" },
                    { "E", "{{F}}" },
                    { "F", "{{G}}" },
                    { "G", "{{H}}" },
                    { "H", "{{A}}" }
                },
                new Dictionary<string, string?> { { "A", "{{A}}" } },
                new Dictionary<string, string?>(),
                new Dictionary<string, string?>(),
                // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
                // no shit I only use this for pre-condition-tests, in a UNIT-TEST
                result => { Assert.Equal("", result.CompiledConfiguration["A"]); });

        /// <summary>
        ///     don't fail when there is nothing to do
        /// </summary>
        [Fact]
        public void NoCompilationNeeded()
            => CheckCompilationResult(
                new Dictionary<string, string?>(),
                new Dictionary<string, string?>(),
                new Dictionary<string, string?>(),
                new Dictionary<string, string?>(),
                result =>
                {
                    IDictionary<string, string?> compiled = result.CompiledConfiguration;

                    Assert.NotNull(compiled);
                    Assert.Empty(compiled);
                });

        /// <summary>
        ///     compile environment-values that have null as value, and preserve them in the output
        /// </summary>
        [Fact]
        public void SectionCompilationPreservesNull()
            => CheckCompilationResult(
                new Dictionary<string, string?>
                {
                    { "SectionWithNull/First", null },
                    { "SectionWithNull/Second", null },
                    { "D", "" }
                },
                new Dictionary<string, string?>
                {
                    { "A", "{{SectionWithNull/*}}" },
                    { "B", "{{D}}" }
                },
                new Dictionary<string, string?>(),
                new Dictionary<string, string?>(),
                result =>
                {
                    IDictionary<string, string?> compiled = result.CompiledConfiguration;

                    Assert.NotNull(compiled);

                    Assert.Equal(3, compiled.Count);
                    Assert.Null(compiled["A/First"]);
                    Assert.Null(compiled["A/Second"]);
                    Assert.Equal(string.Empty, compiled["B"]);
                });

        private void CheckCompilationResult(
            IDictionary<string, string?> keys,
            IDictionary<string, string?> structKeys,
            IDictionary<string, string?> structVars,
            IDictionary<string, string?> secrets,
            Action<CompilationResult> assertions,
            Func<IConfigurationCompiler, IConfigurationParser, EnvironmentCompilationInfo, StructureCompilationInfo, CompilationResult>? compileFunc = null,
            [CallerMemberName] string? testName = null)
        {
            ServiceProvider provider = new ServiceCollection().AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning))
                                                              .AddTransient<ISecretConfigValueProvider, TestSecretProvider>(
                                                                  _ => new TestSecretProvider(secrets))
                                                              .BuildServiceProvider();

            var compilerLogger = provider.GetRequiredService<ILogger<ConfigurationCompiler>>();
            var resolverLogger = provider.GetRequiredService<ILogger<IValueResolver>>();
            var secretProvider = provider.GetRequiredService<ISecretConfigValueProvider>();

            IConfigurationCompiler compiler = new ConfigurationCompiler(secretProvider, compilerLogger, resolverLogger);
            IConfigurationParser parser = new AntlrConfigurationParser();

            var env = new EnvironmentCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string?>(keys),
                Name = $"{testName}-Environment"
            };

            var structure = new StructureCompilationInfo
            {
                Keys = new ReadOnlyDictionary<string, string?>(structKeys),
                Variables = new ReadOnlyDictionary<string, string?>(structVars),
                Name = $"{testName}-Structure"
            };

            CompilationResult compiled = compileFunc?.Invoke(compiler, parser, env, structure) ?? compiler.Compile(env, structure, parser);

            assertions(compiled);
        }

        private class TestSecretProvider : DictionaryValueProvider, ISecretConfigValueProvider
        {
            /// <inheritdoc />
            public TestSecretProvider(IDictionary<string, string?> repository)
                : base(repository, "Test-Secrets")
            {
            }
        }
    }
}
