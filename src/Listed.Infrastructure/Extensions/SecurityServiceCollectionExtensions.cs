using Listed.Application.Contracts.Security;
using Listed.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Listed.Infrastructure.Extensions;

public static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AuthOptions>>().Value;
            options.Validate();
            return options;
        });
        services.AddSingleton<IAuthSettings>(sp => sp.GetRequiredService<AuthOptions>());

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IAuthStateStore, ValkeyAuthStateStore>();
        services.AddScoped<IAccessTokenService, AccessTokenService>();
        services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<JwtTokenValidationService>();

        return services;
    }
}
