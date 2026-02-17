using Listed.API.Abstractions;
using Listed.API.Common.ErrorMapping;

namespace Listed.API.Extensions;

public static class ErrorMappingServiceCollectionExtensions
{
    public static IServiceCollection AddErrorMapping(this IServiceCollection services)
    {
        services.AddScoped<IErrorHttpMapper, UserErrorHttpMapper>();
        services.AddScoped<ResultHttpMapper>();

        return services;
    }
}
