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
        private readonly Dictionary<string, string> _envStorage;
        private readonly Dictionary<string, string> _schemaStorage;

        public InMemoryConfigurationDatabase(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<InMemoryConfigurationDatabase>();
            _envStorage = new Dictionary<string, string>();
            _schemaStorage = new Dictionary<string, string>();
        }

        private ILogger Logger { get; }

        /// <inheritdoc />
        public Task Connect() => Task.CompletedTask;

        /// <inheritdoc />
        public Task ModifyEnvironment(string environmentName, IEnumerable<ConfigKeyAction> actions)
            => ModifyInternal(environmentName, actions, EnvironmentKey, _envStorage);

        public Task ModifySchema(string service, IEnumerable<ConfigKeyAction> actions)
            => ModifyInternal(service, actions, SchemaKey, _schemaStorage);

        private Task ModifyInternal(string prefix,
                                    IEnumerable<ConfigKeyAction> actions,
                                    Func<string, string, string> keyGenerator,
                                    Dictionary<string, string> storage)
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

        private string SchemaKey(string service, string key) => $"{service}/{key}";

        public void Dump(ILogger logger)
        {
            logger.LogDebug($"\r\n{string.Join(";\r\n", _envStorage.Select(kvp => $"{kvp.Key} => {kvp.Value}"))}");
            logger.LogDebug($"\r\n{string.Join(";\r\n", _schemaStorage.Select(kvp => $"{kvp.Key} => {kvp.Value}"))}");
        }
    }
}