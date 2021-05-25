using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Common.Utilities
{
    public static class ApplicationBuilderExtensions
    {
        public static AppConfigContainer StartTweakingWith(this IApplicationBuilder builder,
                                                           ILogger logger = null,
                                                           IConfiguration configuration = null) => new AppConfigContainer
        {
            Builder = builder,
            Configuration = configuration,
            Logger = logger
        };

        public static AppConfigContainer Tweak(this AppConfigContainer container,
                                               Action<IApplicationBuilder> configuration,
                                               string message)
        {
            container.Logger?.LogInformation(message);
            configuration?.Invoke(container.Builder);

            return container;
        }

        public static AppConfigContainer TweakWhen(this AppConfigContainer container,
                                                   Func<IConfiguration, IConfigurationSection> section,
                                                   Action<IApplicationBuilder> configuration,
                                                   string enabledMessage,
                                                   string disabledMessage = null)
            => container.TweakWhen(section.Invoke(container.Configuration).Get<bool>(),
                                   configuration,
                                   enabledMessage,
                                   disabledMessage);

        public static AppConfigContainer TweakWhen(this AppConfigContainer container,
                                                   bool condition,
                                                   Action<IApplicationBuilder> configuration,
                                                   string enabledMessage,
                                                   string disabledMessage = null)
        {
            if (condition)
                container.Tweak(configuration, enabledMessage);
            else if (!string.IsNullOrWhiteSpace(disabledMessage))
                container.Logger?.LogInformation(disabledMessage);

            return container;
        }

        public struct AppConfigContainer
        {
            public IApplicationBuilder Builder { get; set; }

            public IConfiguration Configuration { get; set; }

            public ILogger Logger { get; set; }
        }
    }
}