using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
            logger.LogInformation($"registering DbContext: {typeof(TContext).Name}");

            return services.AddDbContext<TContext>(optionsAction);
        }

        public static IServiceCollection AddScoped<TService, TImplementation>(this IServiceCollection services, ILogger logger)
            where TService : class
            where TImplementation : class, TService
        {
            logger.LogServiceRegistration<TService, TImplementation>(ServiceLifetime.Scoped);

            return services.AddScoped<TService, TImplementation>();
        }

        public static IServiceCollection AddScoped<TService, TImplementation>(this IServiceCollection services,
                                                                              ILogger logger,
                                                                              Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            logger.LogServiceRegistration<TService, TImplementation>(ServiceLifetime.Scoped, true);

            return services.AddScoped<TService, TImplementation>(implementationFactory);
        }

        public static IServiceCollection AddScoped<TImplementation>(this IServiceCollection services,
                                                                    ILogger logger,
                                                                    Func<IServiceProvider, TImplementation> implementationFactory)
            where TImplementation : class
        {
            logger.LogServiceRegistration<TImplementation>(ServiceLifetime.Scoped);

            return services.AddScoped(implementationFactory);
        }

        public static IServiceCollection AddScoped(this IServiceCollection services,
                                                   ILogger logger,
                                                   Type serviceType)
        {
            logger.LogServiceRegistration(serviceType, ServiceLifetime.Scoped);

            return services.AddScoped(serviceType);
        }

        public static IServiceCollection AddScoped(this IServiceCollection services,
                                                   ILogger logger,
                                                   Type serviceType,
                                                   Type implementationType)
        {
            logger.LogServiceRegistration(serviceType, implementationType, ServiceLifetime.Scoped);

            return services.AddScoped(serviceType, implementationType);
        }

        public static IServiceCollection AddSingleton<TService, TImplementation>(this IServiceCollection services, ILogger logger)
            where TService : class
            where TImplementation : class, TService
        {
            logger.LogServiceRegistration<TService, TImplementation>(ServiceLifetime.Singleton);

            return services.AddSingleton<TService, TImplementation>();
        }

        public static IServiceCollection AddSingleton<TService, TImplementation>(this IServiceCollection services,
                                                                                 ILogger logger,
                                                                                 Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            logger.LogServiceRegistration<TService, TImplementation>(ServiceLifetime.Singleton, true);

            return services.AddSingleton<TService, TImplementation>(implementationFactory);
        }

        public static IServiceCollection AddSingleton<TImplementation>(this IServiceCollection services,
                                                                       ILogger logger,
                                                                       Func<IServiceProvider, TImplementation> implementationFactory)
            where TImplementation : class
        {
            logger.LogServiceRegistration<TImplementation>(ServiceLifetime.Singleton, true);

            return services.AddSingleton(implementationFactory);
        }

        public static IServiceCollection AddSingleton(this IServiceCollection services,
                                                      ILogger logger,
                                                      Type serviceType)
        {
            logger.LogServiceRegistration(serviceType, ServiceLifetime.Singleton);

            return services.AddSingleton(serviceType);
        }

        public static IServiceCollection AddSingleton(this IServiceCollection services,
                                                      ILogger logger,
                                                      Type serviceType,
                                                      Type implementationType)
        {
            logger.LogServiceRegistration(serviceType, implementationType, ServiceLifetime.Singleton);

            return services.AddSingleton(serviceType, implementationType);
        }

        public static IServiceCollection AddTransient<TService, TImplementation>(this IServiceCollection services, ILogger logger)
            where TService : class
            where TImplementation : class, TService
        {
            logger.LogServiceRegistration<TService, TImplementation>(ServiceLifetime.Transient);

            return services.AddTransient<TService, TImplementation>();
        }

        public static IServiceCollection AddTransient<TService, TImplementation>(this IServiceCollection services,
                                                                                 ILogger logger,
                                                                                 Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            logger.LogServiceRegistration<TService, TImplementation>(ServiceLifetime.Transient, true);

            return services.AddTransient<TService, TImplementation>(implementationFactory);
        }

        public static IServiceCollection AddTransient<TImplementation>(this IServiceCollection services,
                                                                       ILogger logger,
                                                                       Func<IServiceProvider, TImplementation> implementationFactory)
            where TImplementation : class
        {
            logger.LogServiceRegistration<TImplementation>(ServiceLifetime.Transient);

            return services.AddTransient(implementationFactory);
        }

        public static IServiceCollection AddTransient(this IServiceCollection services,
                                                      ILogger logger,
                                                      Type serviceType)
        {
            logger.LogServiceRegistration(serviceType, ServiceLifetime.Transient);

            return services.AddTransient(serviceType);
        }

        public static IServiceCollection AddTransient(this IServiceCollection services,
                                                      ILogger logger,
                                                      Type serviceType,
                                                      Type implementationType)
        {
            logger.LogServiceRegistration(serviceType, implementationType, ServiceLifetime.Transient);

            return services.AddTransient(serviceType, implementationType);
        }

        private static void LogServiceRegistration<TService, TImplementation>(this ILogger logger,
                                                                              ServiceLifetime lifetime,
                                                                              bool usingCustomFactory = false)
            => logger.LogInformation($"registering {lifetime:G}: " +
                                     $"{typeof(TService).Name} => {typeof(TImplementation).Name}" +
                                     $"{(usingCustomFactory ? " with custom factory-action" : "")}");

        private static void LogServiceRegistration<TImplementation>(this ILogger logger,
                                                                    ServiceLifetime lifetime,
                                                                    bool usingCustomFactory = false)
            => logger.LogInformation($"registering {lifetime:G}: " +
                                     $"{typeof(TImplementation).Name}" +
                                     $"{(usingCustomFactory ? " with custom factory-action" : "")}");

        private static void LogServiceRegistration(this ILogger logger,
                                                   Type serviceType,
                                                   ServiceLifetime lifetime,
                                                   bool usingCustomFactory = false)
            => logger.LogInformation($"registering {lifetime:G}: " +
                                     $"{serviceType.Name}" +
                                     $"{(usingCustomFactory ? " with custom factory-action" : "")}");

        private static void LogServiceRegistration(this ILogger logger,
                                                   Type serviceType,
                                                   Type implementationType,
                                                   ServiceLifetime lifetime,
                                                   bool usingCustomFactory = false)
            => logger.LogInformation($"registering {lifetime:G}: " +
                                     $"{serviceType.Name} => {implementationType.Name}" +
                                     $"{(usingCustomFactory ? " with custom factory-action" : "")}");
    }
}