using Microsoft.Extensions.DependencyInjection;
using NCcsds.Core.Configuration;
using NCcsds.Core.Interfaces;

namespace NCcsds.Core.DependencyInjection;

/// <summary>
/// Extension methods for adding NCcsds.Core services to the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds NCcsds.Core services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNCcsdsCore(this IServiceCollection services)
    {
        // Register validators
        services.AddSingleton<IValidator<TmFrameConfiguration>, TmFrameConfigurationValidator>();
        services.AddSingleton<IValidator<TcFrameConfiguration>, TcFrameConfigurationValidator>();
        services.AddSingleton<IValidator<AosFrameConfiguration>, AosFrameConfigurationValidator>();

        return services;
    }

    /// <summary>
    /// Adds NCcsds.Core services with TM frame configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNCcsdsCore(
        this IServiceCollection services,
        Action<TmFrameConfiguration> configure)
    {
        services.AddNCcsdsCore();
        services.Configure(configure);
        return services;
    }
}
