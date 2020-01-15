using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Parsing;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class PreparedConfigurationTests
    {
        [Fact]
        public void BuildConfiguration()
        {
            var config = new PreparedConfiguration(
                new ConfigurationIdentifier(
                    new EnvironmentIdentifier("Foo", "Bar"),
                    new StructureIdentifier("Foo", 42),
                    4711));

            config.Build(null, null);

            Assert.True(config.Created);
            Assert.False(config.Built);
            Assert.Empty(config.Keys);
            Assert.Empty(config.UsedKeys);
            Assert.InRange(config.ConfigurationVersion, 1, long.MaxValue);
            Assert.Null(config.Json);
        }

        [Fact]
        public void CacheItemPriority()
            => new PreparedConfiguration(
                new ConfigurationIdentifier(
                    new EnvironmentIdentifier("Foo", "Bar"),
                    new StructureIdentifier("Foo", 42),
                    4711
                )).GetCacheItemPriority();

        [Fact]
        public void CalculateCacheSize()
        {
            var item = new PreparedConfiguration(
                new ConfigurationIdentifier(
                    new EnvironmentIdentifier("Foo", "Bar"),
                    new StructureIdentifier("Foo", 42),
                    4711));

            Assert.NotInRange(item.CalculateCacheSize(), long.MinValue, 0);
        }

        [Fact]
        public async Task CompilationFailsWhenEnvironmentNotFound()
        {
            var config = new PreparedConfiguration(
                new ConfigurationIdentifier(
                    new EnvironmentIdentifier("Foo", "Bar"),
                    new StructureIdentifier("Foo", 42),
                    4711));

            var store = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            store.Setup(s => s.ReplayObject(It.IsAny<ConfigEnvironment>(),
                                            It.IsAny<string>(),
                                            It.IsAny<long>()))
                 .ReturnsAsync(() => Result.Error<ConfigEnvironment>(string.Empty, ErrorCode.DbQueryError))
                 .Verifiable("Environment not retrieved");

            var compiler = new Mock<IConfigurationCompiler>(MockBehavior.Strict);
            var parser = new Mock<IConfigurationParser>(MockBehavior.Strict);
            var translator = new Mock<IJsonTranslator>(MockBehavior.Strict);

            var result = await config.Compile(store.Object, compiler.Object, parser.Object, translator.Object);

            Assert.True(result.IsError);

            store.Verify();
        }

        [Fact]
        public async Task CompilationFailsWhenStructureNotFound()
        {
            var config = new PreparedConfiguration(
                new ConfigurationIdentifier(
                    new EnvironmentIdentifier("Foo", "Bar"),
                    new StructureIdentifier("Foo", 42),
                    4711));

            var store = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            store.Setup(s => s.ReplayObject(It.IsAny<ConfigEnvironment>(),
                                            It.IsAny<string>(),
                                            It.IsAny<long>()))
                 .ReturnsAsync((ConfigEnvironment env, string id, long version) =>
                 {
                     env.ApplyEvent(new ReplayedEvent
                     {
                         UtcTime = DateTime.UtcNow,
                         Version = 1,
                         DomainEvent = new EnvironmentKeysImported(env.Identifier, new[]
                         {
                             ConfigKeyAction.Set("Foo", "Bar", "", ""),
                             ConfigKeyAction.Set("Jar", "Jar", "", "")
                         })
                     });
                     return Result.Success(env);
                 })
                 .Verifiable("Environment not retrieved");

            store.Setup(s => s.ReplayObject(It.IsAny<ConfigStructure>(),
                                            It.IsAny<string>(),
                                            It.IsAny<long>()))
                 .ReturnsAsync(() => Result.Error<ConfigStructure>(string.Empty, ErrorCode.DbQueryError))
                 .Verifiable("Structure not retrieved");

            var compiler = new Mock<IConfigurationCompiler>(MockBehavior.Strict);
            var parser = new Mock<IConfigurationParser>(MockBehavior.Strict);
            var translator = new Mock<IJsonTranslator>(MockBehavior.Strict);

            var result = await config.Compile(store.Object, compiler.Object, parser.Object, translator.Object);

            Assert.True(result.IsError);

            store.Verify();
        }

        [Fact]
        public async Task CompilationThrowsOnInvalidArguments()
        {
            var config = new PreparedConfiguration(
                new ConfigurationIdentifier(
                    new EnvironmentIdentifier("Foo", "Bar"),
                    new StructureIdentifier("Foo", 42),
                    4711));

            var store = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            var compiler = new Mock<IConfigurationCompiler>(MockBehavior.Strict);
            var parser = new Mock<IConfigurationParser>(MockBehavior.Strict);
            var translator = new Mock<IJsonTranslator>(MockBehavior.Strict);

            await Assert.ThrowsAsync<ArgumentNullException>(() => config.Compile(null, null, null, null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => config.Compile(store.Object, compiler.Object, parser.Object, null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => config.Compile(store.Object, compiler.Object, null, translator.Object));
            await Assert.ThrowsAsync<ArgumentNullException>(() => config.Compile(store.Object, null, parser.Object, translator.Object));
            await Assert.ThrowsAsync<ArgumentNullException>(() => config.Compile(null, compiler.Object, parser.Object, translator.Object));
        }

        [Fact]
        public async Task ConfigCompilationSucceeds()
        {
            var config = new PreparedConfiguration(
                new ConfigurationIdentifier(
                    new EnvironmentIdentifier("Foo", "Bar"),
                    new StructureIdentifier("Foo", 42),
                    4711));

            var store = new Mock<IDomainObjectStore>(MockBehavior.Strict);

            store.Setup(s => s.ReplayObject(It.IsAny<ConfigEnvironment>(),
                                            It.IsAny<string>(),
                                            It.IsAny<long>()))
                 .ReturnsAsync((ConfigEnvironment env, string id, long version) =>
                 {
                     env.ApplyEvent(new ReplayedEvent
                     {
                         UtcTime = DateTime.UtcNow,
                         Version = 1,
                         DomainEvent = new EnvironmentKeysImported(env.Identifier, new[]
                         {
                             ConfigKeyAction.Set("Foo", "Bar", "", ""),
                             ConfigKeyAction.Set("Jar", "Jar", "", "")
                         })
                     });
                     return Result.Success(env);
                 })
                 .Verifiable("Environment not retrieved");

            store.Setup(s => s.ReplayObject(It.IsAny<ConfigStructure>(),
                                            It.IsAny<string>(),
                                            It.IsAny<long>()))
                 .ReturnsAsync((ConfigStructure str, string id, long version) =>
                 {
                     str.ApplyEvent(new ReplayedEvent
                     {
                         UtcTime = DateTime.UtcNow,
                         Version = 1,
                         DomainEvent = new StructureCreated(str.Identifier,
                                                            new Dictionary<string, string> {{"Ref", "{{Foo}}{{Jar}}"}},
                                                            new Dictionary<string, string>())
                     });
                     return Result.Success(str);
                 })
                 .Verifiable("Structure not retrieved");

            var compiler = new Mock<IConfigurationCompiler>(MockBehavior.Strict);
            compiler.Setup(c => c.Compile(It.IsAny<EnvironmentCompilationInfo>(),
                                          It.IsAny<StructureCompilationInfo>(),
                                          It.IsAny<IConfigurationParser>()))
                    .Returns(() => new CompilationResult(new Dictionary<string, string> {{"Ref", "BarJar"}},
                                                         new TraceResult[0]))
                    .Verifiable("Compilation not triggered");

            var parser = new Mock<IConfigurationParser>(MockBehavior.Strict);

            var translator = new Mock<IJsonTranslator>(MockBehavior.Strict);
            translator.Setup(t => t.ToJson(It.IsAny<IDictionary<string, string>>()))
                      .Returns(() => JsonDocument.Parse("{}").RootElement)
                      .Verifiable("Result not translated to JSON");

            var result = await config.Compile(store.Object,
                                              compiler.Object,
                                              parser.Object,
                                              translator.Object);

            Assert.False(result.IsError);

            store.Verify();
            compiler.Verify();
            parser.Verify();
            translator.Verify();
        }

        [Fact]
        public void Create()
        {
            var item = new PreparedConfiguration(
                new ConfigurationIdentifier(
                    new EnvironmentIdentifier("Foo", "Bar"),
                    new StructureIdentifier("Foo", 42),
                    4711
                ));

            item.Build(null, null);

            Assert.True(item.Created);
            Assert.Empty(item.Keys);
            Assert.Empty(item.UsedKeys);
            Assert.Null(item.Json);
            Assert.Null(item.ValidFrom);
            Assert.Null(item.ValidTo);
            Assert.False(item.Built);
        }

        [Fact]
        public async Task NoThrowOnCompilationException()
        {
            var config = new PreparedConfiguration(
                new ConfigurationIdentifier(
                    new EnvironmentIdentifier("Foo", "Bar"),
                    new StructureIdentifier("Foo", 42),
                    4711));

            var store = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            store.Setup(s => s.ReplayObject(It.IsAny<ConfigEnvironment>(),
                                            It.IsAny<string>(),
                                            It.IsAny<long>()))
                 .ReturnsAsync((ConfigEnvironment env, string id, long version) =>
                 {
                     env.ApplyEvent(new ReplayedEvent
                     {
                         UtcTime = DateTime.UtcNow,
                         Version = 1,
                         DomainEvent = new EnvironmentKeysImported(env.Identifier, new[]
                         {
                             ConfigKeyAction.Set("Foo", "Bar", "", ""),
                             ConfigKeyAction.Set("Jar", "Jar", "", "")
                         })
                     });
                     return Result.Success(env);
                 })
                 .Verifiable("Environment not retrieved");

            store.Setup(s => s.ReplayObject(It.IsAny<ConfigStructure>(),
                                            It.IsAny<string>(),
                                            It.IsAny<long>()))
                 .ReturnsAsync((ConfigStructure str, string id, long version) =>
                 {
                     str.ApplyEvent(new ReplayedEvent
                     {
                         UtcTime = DateTime.UtcNow,
                         Version = 1,
                         DomainEvent = new StructureCreated(str.Identifier,
                                                            new Dictionary<string, string> {{"Ref", "{{Foo}}{{Jar}}"}},
                                                            new Dictionary<string, string>())
                     });
                     return Result.Success(str);
                 })
                 .Verifiable("Structure not retrieved");

            var compiler = new Mock<IConfigurationCompiler>(MockBehavior.Strict);
            compiler.Setup(c => c.Compile(It.IsAny<EnvironmentCompilationInfo>(), It.IsAny<StructureCompilationInfo>(), It.IsAny<IConfigurationParser>()))
                    .Throws<Exception>()
                    .Verifiable();

            var parser = new Mock<IConfigurationParser>(MockBehavior.Strict);
            var translator = new Mock<IJsonTranslator>(MockBehavior.Strict);

            var result = await config.Compile(store.Object, compiler.Object, parser.Object, translator.Object);

            Assert.True(result.IsError);

            store.Verify();
            compiler.Verify();
        }

        [Fact]
        public void ReplayHandlesConfigurationBuilt()
        {
            var config = new PreparedConfiguration(
                new ConfigurationIdentifier(
                    new EnvironmentIdentifier("Foo", "Bar"),
                    new StructureIdentifier("Foo", 42),
                    4711));

            config.ApplyEvent(new ReplayedEvent
            {
                UtcTime = DateTime.UtcNow,
                Version = 1,
                DomainEvent = new ConfigurationBuilt(config.Identifier, null, null)
            });

            Assert.True(config.Created);
            Assert.False(config.Built);
            Assert.Empty(config.Keys);
            Assert.Empty(config.UsedKeys);
            Assert.InRange(config.ConfigurationVersion, 1, long.MaxValue);
            Assert.Null(config.Json);
        }

        [Fact]
        public void ThrowForInvalidIdentifier()
            => Assert.Throws<ArgumentNullException>(() => new PreparedConfiguration(new ConfigurationIdentifier(null, null, 0)));

        [Fact]
        public void ThrowForNullIdentifier() => Assert.Throws<ArgumentNullException>(() => new PreparedConfiguration(null));
    }
}