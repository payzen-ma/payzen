// Extensions: ServiceCollection helpers for registering application services and options
// This file centralizes DI registration helpers used by the refactored Program.cs.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using payzen_backend.Configuration;

namespace payzen_backend.Extensions;

public static class ServiceCollectionExtensions
{
    // Registers strongly typed options using the Options pattern.
    public static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AnthropicOptions>(configuration.GetSection(AnthropicOptions.SectionName));
        services.Configure<GoogleOptions>(configuration.GetSection(GoogleOptions.SectionName));
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        // Example of validating JwtOptions on startup
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    // Register application services here (moved from Program.cs in later phases)
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Service registrations will be moved here in Phase 3.
        return services;
    }

    // Placeholder for FluentValidation registration (Phase 7)
    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        // FluentValidation registration will be added in Phase 7.
        return services;
    }
}
