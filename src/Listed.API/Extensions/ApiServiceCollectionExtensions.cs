using Listed.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Listed.API.Extensions;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IConnectionMultiplexer valkeyConnectionMultiplexer)
    {
        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()
            ?? throw new InvalidOperationException("Auth configuration is missing.");
        authOptions.Validate();

        var dataProtectionConfiguration = LoadDataProtectionConfiguration(configuration);

        services.AddErrorMapping();
        services.AddListedDataProtection(valkeyConnectionMultiplexer, dataProtectionConfiguration);
        services.AddListedForwardedHeaders();
        services.AddListedAuthentication(authOptions);
        services.AddListedSignalR(valkeyConnectionMultiplexer);
        services.AddAuthorization();
        services.AddControllers();

        return services;
    }

    private static IServiceCollection AddListedDataProtection(
        this IServiceCollection services,
        IConnectionMultiplexer valkeyConnectionMultiplexer,
        DataProtectionConfiguration dataProtectionConfiguration)
    {
        var dataProtectionBuilder = services
            .AddDataProtection()
            .SetApplicationName(dataProtectionConfiguration.ApplicationName)
            .PersistKeysToStackExchangeRedis(valkeyConnectionMultiplexer, dataProtectionConfiguration.KeyRingKey);

        if (!dataProtectionConfiguration.HasCertificate)
        {
            return services;
        }

        if (!File.Exists(dataProtectionConfiguration.CertificatePath))
        {
            throw new InvalidOperationException(
                $"DataProtection certificate file was not found at '{dataProtectionConfiguration.CertificatePath}'.");
        }

        var certificate = X509CertificateLoader.LoadPkcs12FromFile(
            dataProtectionConfiguration.CertificatePath,
            dataProtectionConfiguration.CertificatePassword,
            X509KeyStorageFlags.EphemeralKeySet);

        dataProtectionBuilder.ProtectKeysWithCertificate(certificate);

        return services;
    }

    private static IServiceCollection AddListedForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    private static IServiceCollection AddListedAuthentication(this IServiceCollection services, AuthOptions authOptions)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var validator = context.HttpContext.RequestServices.GetRequiredService<JwtTokenValidationService>();
                        await validator.ValidateAsync(context);
                    }
                };
            });

        return services;
    }

    private static IServiceCollection AddListedSignalR(
        this IServiceCollection services,
        IConnectionMultiplexer valkeyConnectionMultiplexer)
    {
        services
            .AddSignalR()
            .AddStackExchangeRedis(options =>
            {
                options.ConnectionFactory = _ => Task.FromResult<IConnectionMultiplexer>(valkeyConnectionMultiplexer);
            });

        return services;
    }

    private static DataProtectionConfiguration LoadDataProtectionConfiguration(IConfiguration configuration)
    {
        var applicationName = configuration["DataProtection:ApplicationName"]
            ?? throw new InvalidOperationException("DataProtection:ApplicationName is missing.");
        var keyRingKey = configuration["DataProtection:KeyRingKey"]
            ?? throw new InvalidOperationException("DataProtection:KeyRingKey is missing.");
        var certificatePath = configuration["DataProtection:CertificatePath"];
        var certificatePassword = configuration["DataProtection:CertificatePassword"];

        if (string.IsNullOrWhiteSpace(applicationName))
        {
            throw new InvalidOperationException("DataProtection:ApplicationName cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(keyRingKey))
        {
            throw new InvalidOperationException("DataProtection:KeyRingKey cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(certificatePath) != string.IsNullOrWhiteSpace(certificatePassword))
        {
            throw new InvalidOperationException(
                "DataProtection:CertificatePath and DataProtection:CertificatePassword must both be set or both be empty.");
        }

        return new DataProtectionConfiguration(applicationName, keyRingKey, certificatePath, certificatePassword);
    }

    private sealed record DataProtectionConfiguration(
        string ApplicationName,
        string KeyRingKey,
        string? CertificatePath,
        string? CertificatePassword)
    {
        public bool HasCertificate => !string.IsNullOrWhiteSpace(CertificatePath);
    }
}
