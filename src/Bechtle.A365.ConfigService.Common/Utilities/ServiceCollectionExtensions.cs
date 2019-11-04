using System;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Common.Utilities
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDbContext<TContext>(this IServiceCollection services,
                                                                ILogger logger,
                                                                Action<IServiceProvider, DbContextOptionsBuilder> optionsAction)
            where TContext : DbContext
        {
            logger.LogInformation($"registering DbContext: {typeof(TContext).GetFriendlyName()}");

            return services.AddDbContext<TContext>(optionsAction);
        }

        public static IServiceCollection AddHostedService<THostedService>(this IServiceCollection services, ILogger logger)
            where THostedService : class, IHostedService
        {
            logger.LogServiceRegistration(ServiceLifetime.Singleton,
                                          typeof(IHostedService).GetFriendlyName(),
                                          typeof(THostedService).GetFriendlyName());

            return services.AddHostedService<THostedService>();
        }

        public static IServiceCollection AddScoped<TService>(this IServiceCollection services, ILogger logger)
            where TService : class
        {
            logger.LogServiceRegistration(ServiceLifetime.Scoped,
                                          typeof(TService).GetFriendlyName());

            return services.AddScoped<TService>();
        }

        public static IServiceCollection AddScoped<TService, TImplementation>(this IServiceCollection services, ILogger logger)
            where TService : class
            where TImplementation : class, TService
        {
            logger.LogServiceRegistration(ServiceLifetime.Scoped,
                                          typeof(TService).GetFriendlyName(),
                                          typeof(TImplementation).GetFriendlyName());

            return services.AddScoped<TService, TImplementation>();
        }

        public static IServiceCollection AddScoped<TService, TImplementation>(this IServiceCollection services,
                                                                              ILogger logger,
                                                                              Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            logger.LogServiceRegistration(ServiceLifetime.Scoped,
                                          typeof(TService).GetFriendlyName(),
                                          typeof(TImplementation).GetFriendlyName(),
                                          true);

            return services.AddScoped<TService, TImplementation>(implementationFactory);
        }

        public static IServiceCollection AddScoped<TImplementation>(this IServiceCollection services,
                                                                    ILogger logger,
                                                                    Func<IServiceProvider, TImplementation> implementationFactory)
            where TImplementation : class
        {
            logger.LogServiceRegistration(ServiceLifetime.Scoped,
                                          typeof(TImplementation).GetFriendlyName(),
                                          usingCustomFactory: true);

            return services.AddScoped(implementationFactory);
        }

        public static IServiceCollection AddScoped(this IServiceCollection services,
                                                   ILogger logger,
                                                   Type serviceType)
        {
            logger.LogServiceRegistration(ServiceLifetime.Scoped,
                                          serviceType.GetFriendlyName());

            return services.AddScoped(serviceType);
        }

        public static IServiceCollection AddScoped(this IServiceCollection services,
                                                   ILogger logger,
                                                   Type serviceType,
                                                   Type implementationType)
        {
            logger.LogServiceRegistration(ServiceLifetime.Scoped,
                                          serviceType.GetFriendlyName(),
                                          implementationType.GetFriendlyName());

            return services.AddScoped(serviceType, implementationType);
        }

        public static IServiceCollection AddScoped<T>(this IServiceCollection services,
                                                      ILogger logger,
                                                      T implementationInstance)
            where T : class
        {
            logger.LogServiceRegistration(ServiceLifetime.Scoped,
                                          typeof(T).GetFriendlyName(),
                                          usingCustomInstance: true);

            return services.AddSingleton(implementationInstance);
        }

        public static IServiceCollection AddSingleton<TService, TImplementation>(this IServiceCollection services, ILogger logger)
            where TService : class
            where TImplementation : class, TService
        {
            logger.LogServiceRegistration(ServiceLifetime.Singleton,
                                          typeof(TService).GetFriendlyName(),
                                          typeof(TImplementation).GetFriendlyName());

            return services.AddSingleton<TService, TImplementation>();
        }

        public static IServiceCollection AddSingleton<TService, TImplementation>(this IServiceCollection services,
                                                                                 ILogger logger,
                                                                                 Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            logger.LogServiceRegistration(ServiceLifetime.Singleton,
                                          typeof(TService).GetFriendlyName(),
                                          typeof(TImplementation).GetFriendlyName(),
                                          true);

            return services.AddSingleton<TService, TImplementation>(implementationFactory);
        }

        public static IServiceCollection AddSingleton<TImplementation>(this IServiceCollection services,
                                                                       ILogger logger,
                                                                       Func<IServiceProvider, TImplementation> implementationFactory)
            where TImplementation : class
        {
            logger.LogServiceRegistration(ServiceLifetime.Singleton,
                                          typeof(TImplementation).GetFriendlyName(),
                                          usingCustomFactory: true);

            return services.AddSingleton(implementationFactory);
        }

        public static IServiceCollection AddSingleton(this IServiceCollection services,
                                                      ILogger logger,
                                                      Type serviceType)
        {
            logger.LogServiceRegistration(ServiceLifetime.Singleton,
                                          serviceType.GetFriendlyName());

            return services.AddSingleton(serviceType);
        }

        public static IServiceCollection AddSingleton(this IServiceCollection services,
                                                      ILogger logger,
                                                      Type serviceType,
                                                      Type implementationType)
        {
            logger.LogServiceRegistration(ServiceLifetime.Singleton,
                                          serviceType.GetFriendlyName(),
                                          implementationType.GetFriendlyName());

            return services.AddSingleton(serviceType, implementationType);
        }

        public static IServiceCollection AddSingleton<T>(this IServiceCollection services,
                                                         ILogger logger,
                                                         T implementationInstance)
            where T : class
        {
            logger.LogServiceRegistration(ServiceLifetime.Singleton,
                                          typeof(T).GetFriendlyName(),
                                          usingCustomInstance: true);

            return services.AddSingleton(implementationInstance);
        }

        public static IServiceCollection AddTransient<TService, TImplementation>(this IServiceCollection services, ILogger logger)
            where TService : class
            where TImplementation : class, TService
        {
            logger.LogServiceRegistration(ServiceLifetime.Transient,
                                          typeof(TService).GetFriendlyName(),
                                          typeof(TImplementation).GetFriendlyName());

            return services.AddTransient<TService, TImplementation>();
        }

        public static IServiceCollection AddTransient<TService, TImplementation>(this IServiceCollection services,
                                                                                 ILogger logger,
                                                                                 Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            logger.LogServiceRegistration(ServiceLifetime.Transient,
                                          typeof(TService).GetFriendlyName(),
                                          typeof(TImplementation).GetFriendlyName(),
                                          true);

            return services.AddTransient<TService, TImplementation>(implementationFactory);
        }

        public static IServiceCollection AddTransient<TImplementation>(this IServiceCollection services,
                                                                       ILogger logger,
                                                                       Func<IServiceProvider, TImplementation> implementationFactory)
            where TImplementation : class
        {
            logger.LogServiceRegistration(ServiceLifetime.Transient,
                                          typeof(TImplementation).GetFriendlyName(),
                                          usingCustomFactory: true);

            return services.AddTransient(implementationFactory);
        }

        public static IServiceCollection AddTransient(this IServiceCollection services,
                                                      ILogger logger,
                                                      Type serviceType)
        {
            logger.LogServiceRegistration(ServiceLifetime.Transient,
                                          serviceType.GetFriendlyName());

            return services.AddTransient(serviceType);
        }

        public static IServiceCollection AddTransient(this IServiceCollection services,
                                                      ILogger logger,
                                                      Type serviceType,
                                                      Type implementationType)
        {
            logger.LogServiceRegistration(ServiceLifetime.Transient,
                                          serviceType.GetFriendlyName(),
                                          implementationType.GetFriendlyName());

            return services.AddTransient(serviceType, implementationType);
        }

        private static string GetFriendlyName(this Type type)
        {
            var friendlyName = type.Name;

            if (!type.IsGenericType)
                return friendlyName;

            var backtickIndex = friendlyName.IndexOf('`');

            if (backtickIndex > 0)
                friendlyName = friendlyName.Remove(backtickIndex);

            friendlyName += "<";

            var typeParameters = type.GetGenericArguments();

            for (var i = 0; i < typeParameters.Length; ++i)
            {
                var typeParamName = GetFriendlyName(typeParameters[i]);
                friendlyName += i == 0 ? typeParamName : "," + typeParamName;
            }

            friendlyName += ">";

            return friendlyName;
        }

        private static void LogServiceRegistration(this ILogger logger,
                                                   ServiceLifetime lifetime,
                                                   string serviceName,
                                                   string implementationName = "",
                                                   bool usingCustomFactory = false,
                                                   bool usingCustomInstance = false)
        {
            var messageBuilder = new StringBuilder();

            messageBuilder.Append($"registering {lifetime:G}: ");
            messageBuilder.Append(serviceName);

            if (!string.IsNullOrWhiteSpace(implementationName))
                messageBuilder.Append($" => {implementationName}");

            if (usingCustomFactory)
                messageBuilder.Append(", using a custom factory");

            if (usingCustomInstance)
                messageBuilder.Append(", using a pre-built instance");

            logger.LogInformation(messageBuilder.ToString());
        }
    }
}