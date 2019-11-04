using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Services;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Domain-Object representing a Configuration built from a Structure and an Environment
    /// </summary>
    public class StreamedConfiguration : StreamedObject
    {
        private readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 1, 1, 1, DateTimeKind.Utc);

        /// <inheritdoc />
        public StreamedConfiguration(ConfigurationIdentifier identifier)
        {
            Identifier = identifier;
        }

        /// <summary>
        ///     flag indicating if this Configuration has been built or not
        /// </summary>
        public bool Built { get; protected set; }

        /// <summary>
        ///     Data-Version from which this Configuration was built
        /// </summary>
        public long ConfigurationVersion { get; protected set; }

        /// <inheritdoc cref="ConfigurationIdentifier" />
        public ConfigurationIdentifier Identifier { get; protected set; }

        /// <summary>
        ///     Actual Data built from this Configuration, as JSON
        /// </summary>
        public JsonElement? Json { get; protected set; }

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
            ValidFrom = validFrom;
            ValidTo = validTo;
            Built = false;
            Keys = new Dictionary<string, string>();
            Json = null;
            CapturedDomainEvents.Add(new ConfigurationBuilt(Identifier, validFrom, validTo));

            return Result.Success();
        }

        // base cost of 15 (10 from Identifier, 5 for rest)
        // count Keys twice (to estimate cost of Json)
        // count each used key
        /// <inheritdoc />
        public override long CalculateCacheSize()
            => 15
               + (Keys?.Sum(p => p.Key.Length + p.Value.Length) * 2 ?? 0)
               + (UsedKeys?.Sum(s => s.Length) ?? 0);

        /// <summary>
        ///     Compile the configuration that this object represents - subsequent calls will skip recompilation
        /// </summary>
        /// <param name="store"></param>
        /// <param name="compiler"></param>
        /// <param name="parser"></param>
        /// <param name="translator"></param>
        /// <param name="logger">optional logger to pass during the compilation-phase</param>
        /// <returns></returns>
        public async Task<IResult> Compile(IStreamedStore store,
                                           IConfigurationCompiler compiler,
                                           IConfigurationParser parser,
                                           IJsonTranslator translator,
                                           ILogger logger = null)
        {
            if (Built)
                return Result.Success();

            var envResult = await store.GetEnvironment(Identifier.Environment, CurrentVersion);
            if (envResult.IsError)
                return envResult;

            var structResult = await store.GetStructure(Identifier.Structure, CurrentVersion);
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
                Json = translator.ToJson(Keys);
                UsedKeys = compilationResult.GetUsedKeys().ToList();

                return Result.Success();
            }
            catch (Exception e)
            {
                logger?.LogWarning(e, "failed to compile configuration, see exception for more details");
                return Result.Error($"failed to compile configuration: {e.Message}", ErrorCode.InvalidData);
            }
        }

        /// <inheritdoc />
        protected override bool ApplyEventInternal(StreamedEvent streamedEvent)
        {
            switch (streamedEvent.DomainEvent)
            {
                case ConfigurationBuilt built when built.Identifier == Identifier:
                    ValidFrom = built.ValidFrom;
                    ValidTo = built.ValidTo;
                    ConfigurationVersion = (long) streamedEvent.UtcTime
                                                               .Subtract(_unixEpoch)
                                                               .TotalSeconds;
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        protected override void ApplySnapshotInternal(StreamedObject streamedObject)
        {
            if (!(streamedObject is StreamedConfiguration other))
                return;

            Identifier = other.Identifier;
            ValidFrom = other.ValidFrom;
            ValidTo = other.ValidTo;
        }

        /// <inheritdoc />
        protected override string GetSnapshotIdentifier() => Identifier.ToString();
    }
}