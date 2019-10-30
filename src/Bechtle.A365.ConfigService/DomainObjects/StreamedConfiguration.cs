using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Services;
using Bechtle.A365.ConfigService.Services.Stores;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    public class StreamedConfiguration : StreamedObject
    {
        private readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 1, 1, 1, DateTimeKind.Utc);

        public ConfigurationIdentifier Identifier { get; protected set; }

        public bool Built { get; protected set; }

        public DateTime? ValidFrom { get; protected set; }

        public DateTime? ValidTo { get; protected set; }

        public IDictionary<string, string> Keys { get; protected set; }

        public JsonElement? Json { get; protected set; }

        public List<string> UsedKeys { get; protected set; }

        public long ConfigurationVersion { get; protected set; }

        /// <inheritdoc />
        public StreamedConfiguration(ConfigurationIdentifier identifier)
        {
            Identifier = identifier;
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
        public override void ApplySnapshot(StreamedObjectSnapshot snapshot)
        {
            if (snapshot.DataType != GetType().Name)
                return;

            var other = JsonSerializer.Deserialize<StreamedConfiguration>(snapshot.Data);

            Identifier = other.Identifier;
            ValidFrom = other.ValidFrom;
            ValidTo = other.ValidTo;
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
    }
}