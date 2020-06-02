using System;
using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations.Stores
{
    public class ConfiguredSecretStoreTests
    {
        private static ISecretConfigValueProvider BuildProviderWithConfig(IEnumerable<KeyValuePair<string, string>> data)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            var services = new ServiceCollection();
            services.AddOptions()
                    .AddScoped<ISecretConfigValueProvider, ConfiguredSecretStore>()
                    .Configure<ConfiguredSecretStoreConfiguration>(configuration);

            services.AddScoped<ISecretConfigValueProvider, ConfiguredSecretStore>();

            var provider = services.BuildServiceProvider();

            return provider.GetRequiredService<ISecretConfigValueProvider>();
        }

        /// <summary>
        ///     make sure that creating nested keys deeper than "Secrets" will fail.
        ///     this would try to set properties of the "Secrets" dictionary
        /// </summary>
        [Fact]
        public void NestedConfigWithColons() => Assert.Throws<InvalidOperationException>(() => BuildProviderWithConfig(new[]
        {
            new KeyValuePair<string, string>("Secrets", null),
            new KeyValuePair<string, string>("Secrets:Answer:Everything", "42")
        }));

        /// <summary>
        ///     make sure that slashes are preserved when converting "Secrets" to a dictionary
        /// </summary>
        [Fact]
        public void NestedConfigWithSlashes() => Assert.NotNull(BuildProviderWithConfig(new[]
        {
            new KeyValuePair<string, string>("Secrets", null),
            new KeyValuePair<string, string>("Secrets:Answer/Everything", "42")
        }));

        /// <summary>
        ///     test if query can be made with ':' even if config contains '/' (sanitized input)
        /// </summary>
        [Fact]
        public void QueryPropWithColons()
        {
            var provider = BuildProviderWithConfig(new[]
            {
                new KeyValuePair<string, string>("Secrets", null),
                new KeyValuePair<string, string>("Secrets:Answer/Everything", "42")
            });

            Assert.NotNull(provider.TryGetValue("Answer:Everything"));
        }

        /// <summary>
        ///     test if query can be made with '/' when config contains '/' (normal behaviour)
        /// </summary>
        [Fact]
        public void QueryPropWithSlashes()
        {
            var provider = BuildProviderWithConfig(new[]
            {
                new KeyValuePair<string, string>("Secrets", null),
                new KeyValuePair<string, string>("Secrets:Answer/Everything", "42")
            });

            Assert.NotNull(provider.TryGetValue("Answer/Everything"));
        }
    }
}