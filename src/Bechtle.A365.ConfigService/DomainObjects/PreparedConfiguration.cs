using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Parsing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Domain-Object representing a Configuration built from a Structure and an Environment
    /// </summary>
    public class PreparedConfiguration : DomainObject
    {
        private readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 1, 1, 1, DateTimeKind.Utc);

        /// <inheritdoc />
        public PreparedConfiguration(ConfigurationIdentifier identifier)
        {
            if (identifier is null)
                throw new ArgumentNullException(nameof(identifier));

            if (identifier.Environment is null)
                throw new ArgumentNullException(nameof(identifier), $"{nameof(identifier.Environment)} is null");

            if (identifier.Structure is null)
                throw new ArgumentNullException(nameof(identifier), $"{nameof(identifier.Structure)} is null");

            Identifier = identifier;
            Created = false;
            Built = false;
            ConfigurationVersion = -1;
            Json = string.Empty;
            Keys = new Dictionary<string, string>();
            UsedKeys = new List<string>();
            ValidFrom = null;
            ValidTo = null;
        }

        /// <summary>
        ///     flag indicating if this Configuration has been built or not
        /// </summary>
        public bool Built { get; protected set; }

        /// <summary>
        ///     Data-Version from which this Configuration was built
        /// </summary>
        public long ConfigurationVersion { get; protected set; }

        /// <summary>
        ///     flag indicating if this Configuration has been created with a <see cref="ConfigurationBuilt" /> event
        /// </summary>
        public bool Created { get; protected set; }

        /// <inheritdoc cref="ConfigurationIdentifier" />
        public ConfigurationIdentifier Identifier { get; protected set; }

        /// <summary>
        ///     Actual Data built from this Configuration, as JSON
        /// </summary>
        public string Json { get; protected set; }

        /// <summary>
        ///     Actual Data built from this Configuration, as Key=>Value pair
        /// </summary>
        public IDictionary<string, string> Keys { get; protected set; }

        /// <summary>
        ///     List of Environment-Keys used to build this Configuration
        /// </summary>
        public List<string> UsedKeys { get; protected set; }

        /// <summary>
        ///     Starting-Time from which this Configuration is Valid
        /// </summary>
        public DateTime? ValidFrom { get; protected set; }

        /// <summary>
        ///     End-Time until which this Configuration is Valid
        /// </summary>
        public DateTime? ValidTo { get; protected set; }

        /// <summary>
        ///     build this Configuration with the given time-parameters
        /// </summary>
        /// <param name="validFrom"></param>
        /// <param name="validTo"></param>
        /// <returns></returns>
        public IResult Build(DateTime? validFrom, DateTime? validTo)
        {
            Created = true;
            ValidFrom = validFrom;
            ValidTo = validTo;
            Built = false;
            Keys = new Dictionary<string, string>();
            Json = null;
            ConfigurationVersion = (long) DateTime.UtcNow
                                                  .Subtract(_unixEpoch)
                                                  .TotalSeconds;
            CapturedDomainEvents.Add(new ConfigurationBuilt(Identifier, validFrom, validTo));

            return Result.Success();
        }

        // base cost of 15 (10 from Identifier, 5 for rest)
        // count Keys twice (to estimate cost of Json)
        // count each used key
        /// <inheritdoc />
        public override long CalculateCacheSize()
            => 15
               + (Keys?.Sum(p => p.Key?.Length ?? 0 + p.Value?.Length ?? 0) * 2 ?? 0)
               + (UsedKeys?.Sum(s => s?.Length ?? 0) ?? 0);

        /// <summary>
        ///     Compile the configuration that this object represents - subsequent calls will skip recompilation
        /// </summary>
        /// <param name="store"></param>
        /// <param name="compiler"></param>
        /// <param name="parser"></param>
        /// <param name="translator"></param>
        /// <param name="logger">optional logger to pass during the compilation-phase</param>
        /// <param name="assumeLatestVersion">
        ///     set to true, to use latest available versions of Environment and Structure instead of <see cref="DomainObject.CurrentVersion" />
        /// </param>
        /// <returns></returns>
        public async Task<IResult> Compile(IDomainObjectStore store,
                                           IConfigurationCompiler compiler,
                                           IConfigurationParser parser,
                                           IJsonTranslator translator,
                                           ILogger logger = null,
                                           bool assumeLatestVersion = false)
        {
            if (Built)
                return Result.Success();

            CheckCompileParameters(store, compiler, parser, translator);

            var compilationVersion = assumeLatestVersion ? long.MaxValue : CurrentVersion;

            logger?.LogDebug($"version used during compilation: {compilationVersion} ({nameof(assumeLatestVersion)}: {assumeLatestVersion})");

            var envResult = await store.ReplayObject(new ConfigEnvironment(Identifier.Environment),
                                                     Identifier.Environment.ToString(),
                                                     compilationVersion);
            if (envResult.IsError)
                return envResult;

            var structResult = await store.ReplayObject(new ConfigStructure(Identifier.Structure),
                                                        Identifier.Structure.ToString(),
                                                        compilationVersion);
            if (structResult.IsError)
                return structResult;

            var environment = envResult.Data;
            var structure = structResult.Data;

            try
            {
                var compilationResult = compiler.Compile(
                    new EnvironmentCompilationInfo
                    {
                        Name = $"{Identifier.Environment.Category}/{Identifier.Environment.Name}",
                        Keys = environment.GetKeysAsDictionary()
                    },
                    new StructureCompilationInfo
                    {
                        Name = $"{Identifier.Structure.Name}/{Identifier.Structure.Version}",
                        Keys = structure.Keys,
                        Variables = structure.Variables
                    },
                    parser);

                Keys = compilationResult.CompiledConfiguration;
                Json = translator.ToJson(Keys).ToString();
                UsedKeys = compilationResult.GetUsedKeys().ToList();
                Built = true;

                return Result.Success();
            }
            catch (Exception e)
            {
                logger?.LogWarning(e, "failed to compile configuration, see exception for more details");
                return Result.Error($"failed to compile configuration: {e.Message}", ErrorCode.InvalidData);
            }
        }

        /// <inheritdoc />
        public override CacheItemPriority GetCacheItemPriority() => CacheItemPriority.High;

        /// <inheritdoc />
        protected override void ApplySnapshotInternal(DomainObject domainObject)
        {
            if (!(domainObject is PreparedConfiguration other))
                return;

            Created = other.Created;
            Identifier = other.Identifier;
            ValidFrom = other.ValidFrom;
            ValidTo = other.ValidTo;
            Built = other.Built;
            Json = other.Json;
            Keys = other.Keys;
            UsedKeys = other.UsedKeys;
            ConfigurationVersion = other.ConfigurationVersion;
        }

        /// <inheritdoc />
        protected override IDictionary<Type, Func<ReplayedEvent, bool>> GetEventApplicationMapping()
            => new Dictionary<Type, Func<ReplayedEvent, bool>>
            {
                {typeof(ConfigurationBuilt), HandleConfigurationBuiltEvent}
            };

        /// <inheritdoc />
        protected override string GetSnapshotIdentifier() => Identifier.ToString();

        // ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
        private void CheckCompileParameters(IDomainObjectStore store,
                                            IConfigurationCompiler compiler,
                                            IConfigurationParser parser,
                                            IJsonTranslator translator)
        {
            if (store is null) throw new ArgumentNullException(nameof(store));
            if (compiler is null) throw new ArgumentNullException(nameof(compiler));
            if (parser is null) throw new ArgumentNullException(nameof(parser));
            if (translator is null) throw new ArgumentNullException(nameof(translator));
        }
        // ReSharper restore ParameterOnlyUsedForPreconditionCheck.Local

        private bool HandleConfigurationBuiltEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is ConfigurationBuilt built) || built.Identifier != Identifier)
                return false;

            Created = true;
            ValidFrom = built.ValidFrom;
            ValidTo = built.ValidTo;
            Built = false;
            Keys = new Dictionary<string, string>();
            Json = null;
            ConfigurationVersion = (long) replayedEvent.UtcTime
                                                       .Subtract(_unixEpoch)
                                                       .TotalSeconds;
            return true;
        }
    }
}