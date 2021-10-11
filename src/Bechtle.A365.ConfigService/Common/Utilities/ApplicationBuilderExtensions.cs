using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Common.Utilities
{
    /// <summary>
    ///     Extensions for dealing with <see cref="IApplicationBuilder" />
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        ///     Create a new instance of <see cref="AppConfigContainer" /> for the given <see cref="IApplicationBuilder" />
        /// </summary>
        /// <param name="builder">instance of <see cref="IApplicationBuilder" /> that is wrapped in <see cref="AppConfigContainer" /></param>
        /// <param name="configuration">configuration added to this <see cref="AppConfigContainer" /></param>
        /// <param name="logger">logger added to this <see cref="AppConfigContainer" /></param>
        /// <returns>configured instance of <see cref="AppConfigContainer" /></returns>
        public static AppConfigContainer StartTweakingWith(
            this IApplicationBuilder builder,
            IConfiguration configuration,
            ILogger? logger = null) => new()
        {
            Builder = builder,
            Configuration = configuration,
            Logger = logger
        };

        /// <summary>
        ///     Modify settings within the <see cref="IApplicationBuilder" /> used to create <paramref name="container" />
        /// </summary>
        /// <param name="container">App-Container wrapped around the <see cref="IApplicationBuilder" /> that is being configured</param>
        /// <param name="configuration">action that tweaks the App-Configuration</param>
        /// <param name="message">message to log before executing <paramref name="configuration" /></param>
        /// <returns>modified instance of <paramref name="container" /></returns>
        public static AppConfigContainer Tweak(
            this AppConfigContainer container,
            Action<IApplicationBuilder> configuration,
            string message)
        {
            container.Logger?.LogInformation(message);
            configuration.Invoke(container.Builder);

            return container;
        }

        /// <summary>
        ///     Overload of <see cref="Tweak" /> that is only executed when a given Configuration-Setting evaluates to true
        /// </summary>
        /// <param name="container">App-Container wrapped around the <see cref="IApplicationBuilder" /> that is being configured</param>
        /// <param name="section">Function returning a Configuration-Section that can be evaluated</param>
        /// <param name="configuration">action that tweaks the App-Configuration</param>
        /// <param name="enabledMessage">message to log before executing <paramref name="configuration" /></param>
        /// <param name="disabledMessage">message to log when <paramref name="section" /> evaluates to false</param>
        /// <returns>modified instance of <paramref name="container" /></returns>
        public static AppConfigContainer TweakWhen(
            this AppConfigContainer container,
            Func<IConfiguration, IConfigurationSection> section,
            Action<IApplicationBuilder> configuration,
            string enabledMessage,
            string? disabledMessage = null)
            => container.TweakWhen(
                section.Invoke(container.Configuration).Get<bool>(),
                configuration,
                enabledMessage,
                disabledMessage);

        /// <summary>
        ///     Overload of <see cref="Tweak" /> that is only executed when a given Configuration-Setting evaluates to true
        /// </summary>
        /// <param name="container">App-Container wrapped around the <see cref="IApplicationBuilder" /> that is being configured</param>
        /// <param name="condition">condition to evaluate before executing <paramref name="configuration" /></param>
        /// <param name="configuration">action that tweaks the App-Configuration</param>
        /// <param name="enabledMessage">message to log before executing <paramref name="configuration" /></param>
        /// <param name="disabledMessage">message to log when <paramref name="condition" /> evaluates to false</param>
        /// <returns>modified instance of <paramref name="container" /></returns>
        public static AppConfigContainer TweakWhen(
            this AppConfigContainer container,
            bool condition,
            Action<IApplicationBuilder> configuration,
            string enabledMessage,
            string? disabledMessage = null)
        {
            if (condition)
            {
                container.Tweak(configuration, enabledMessage);
            }
            else if (!string.IsNullOrWhiteSpace(disabledMessage))
            {
                container.Logger?.LogInformation(disabledMessage);
            }

            return container;
        }

        /// <summary>
        ///     Wrapper around a <see cref="IApplicationBuilder" /> to act as Context during Configuration
        /// </summary>
        public struct AppConfigContainer
        {
            /// <summary>
            ///     Wrapped instance of <see cref="IApplicationBuilder" />
            /// </summary>
            public IApplicationBuilder Builder { get; set; }

            /// <summary>
            ///     Accompanying configuration used to evaluate conditions
            /// </summary>
            public IConfiguration Configuration { get; set; }

            /// <summary>
            ///     optional Logger to write diagnostics to
            /// </summary>
            public ILogger? Logger { get; set; }
        }
    }
}
