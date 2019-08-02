using System;
using Microsoft.AspNetCore.Builder;

namespace Bechtle.A365.ConfigService.Common.Utilities
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder Configure(this IApplicationBuilder app,
                                                    Action<IApplicationBuilder> configureAction,
                                                    Action<IApplicationBuilder> logAction)
        {
            logAction?.Invoke(app);
            configureAction?.Invoke(app);

            return app;
        }
    }
}