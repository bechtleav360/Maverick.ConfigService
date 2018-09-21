using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bechtle.A365.ConfigService.Projection
{
    public class ConfigurationCompiler : IConfigurationCompiler
    {
        private static RegexOptions StaticRegexOptions = RegexOptions.Compiled |
                                                         RegexOptions.IgnoreCase |
                                                         RegexOptions.CultureInvariant;

        private static Regex MatchReference = new Regex(@"\[(\$(?<Env>[\w\d_\-]+)\/)?(?<Expression>[\w\d_\-\*\/]+)\]", StaticRegexOptions);

        /// <inheritdoc />
        public async Task<IDictionary<string, string>> Compile(string environmentName, string schema, IConfigurationDatabase database)
        {
            var envCache = new EnvironmentCache(database);

            var schemaData = await database.GetSchema(schema);

            var changeList = new Dictionary<string, string>();

            foreach (var kvp in schemaData)
            {
                var referenceMatch = MatchReference.Match(kvp.Value);

                if (referenceMatch.Success)
                    changeList[kvp.Key] = await ResolveReference(environmentName, referenceMatch, envCache);
            }

            foreach (var change in changeList)
                schemaData[change.Key] = change.Value;

            return schemaData;
        }

        //@TODO replace $ENV with the actual current environment... (baseEnvironment)
        private async Task<string> ResolveReference(string baseEnvironment, Match match, EnvironmentCache envCache)
        {
            var envName = match.Groups["Env"].Value;

            if (envName.Equals("Env", StringComparison.InvariantCultureIgnoreCase))
                envName = baseEnvironment;

            var expression = match.Groups["Expression"].Value;

            if (string.IsNullOrWhiteSpace(envName))
                throw new Exception($"no context available, and expression does not contain an environment-reference");

            var envData = await envCache.Get(envName);

            var envExpression = $"{envName}/{expression}";
            var envKey = envData.Keys.FirstOrDefault(k => k == envExpression);

            if (envKey == null)
                throw new Exception($"no key '{expression}' found in Env '{envName}'");

            return envData[envKey];
        }

        private class EnvironmentCache
        {
            private readonly IConfigurationDatabase _database;

            private readonly IDictionary<string, IDictionary<string, string>> _cache;

            public EnvironmentCache(IConfigurationDatabase database)
            {
                _database = database;
                _cache = new Dictionary<string, IDictionary<string, string>>();
            }

            public async Task<IDictionary<string, string>> Get(string environment)
            {
                if (!_cache.ContainsKey(environment))
                {
                    var data = await _database.GetEnvironment(environment);
                    _cache[environment] = data;
                }

                return _cache[environment];
            }
        }
    }
}