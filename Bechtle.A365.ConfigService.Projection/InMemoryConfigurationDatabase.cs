using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection
{
    public class InMemoryConfigurationDatabase : IConfigurationDatabase
    {
        private readonly IDictionary<string, string> _envStorage;
        private readonly IDictionary<string, string> _schemaStorage;
        private readonly IDictionary<string, IDictionary<string, string>> _compiledVersions;

        public InMemoryConfigurationDatabase()
        {
            _envStorage = new Dictionary<string, string>();
            _schemaStorage = new Dictionary<string, string>();
            _compiledVersions = new Dictionary<string, IDictionary<string, string>>();
        }

        /// <inheritdoc />
        public Task Connect() => Task.CompletedTask;

        /// <inheritdoc />
        public Task ModifyEnvironment(string environmentName, IEnumerable<ConfigKeyAction> actions)
            => ModifyInternal(environmentName, actions, EnvironmentKey, _envStorage);

        /// <inheritdoc />
        public Task ModifySchema(string service, IEnumerable<ConfigKeyAction> actions)
            => ModifyInternal(service, actions, SchemaKey, _schemaStorage);

        /// <inheritdoc />
        public Task<IDictionary<string, string>> GetEnvironment(string environmentName)
            => Task.FromResult((IDictionary<string, string>) _envStorage.Where(kvp => kvp.Key.StartsWith(environmentName))
                                                                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

        /// <inheritdoc />
        public Task<IDictionary<string, string>> GetSchema(string schema)
            => Task.FromResult((IDictionary<string, string>) _schemaStorage.Where(kvp => kvp.Key.StartsWith(schema))
                                                                           .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

        public Task SaveCompiledVersion(string environmentName, string schema, IDictionary<string, string> compiledVersion)
        {
            var compositeKey = CompiledVersionKey(environmentName, schema);

            _compiledVersions[compositeKey] = compiledVersion;

            return Task.CompletedTask;
        }

        private Task ModifyInternal(string prefix,
                                    IEnumerable<ConfigKeyAction> actions,
                                    Func<string, string, string> keyGenerator,
                                    IDictionary<string, string> storage)
        {
            foreach (var action in actions)
                switch (action.Type)
                {
                    case ConfigKeyActionType.Set:
                        storage[keyGenerator(prefix, action.Key)] = action.Value;
                        break;

                    case ConfigKeyActionType.Delete:
                        storage.Remove(keyGenerator(prefix, action.Key));
                        break;

                    default:
                        throw new ArgumentOutOfRangeException($"{action.Type:D} {action.Type:G}");
                }

            return Task.CompletedTask;
        }

        private string EnvironmentKey(string environmentName, string key) => $"{environmentName}/{key}";

        private string SchemaKey(string schema, string key) => $"{schema}/{key}";

        private string CompiledVersionKey(string environmentName, string schema) => $"[{environmentName}:{schema}]";

        public void Dump(ILogger logger)
        {
            logger.LogDebug($"\r\n{string.Join(";\r\n", _envStorage.Select(kvp => $"{kvp.Key} => {kvp.Value}"))}");
            logger.LogDebug($"\r\n{string.Join(";\r\n", _schemaStorage.Select(kvp => $"{kvp.Key} => {kvp.Value}"))}");
        }
    }
}