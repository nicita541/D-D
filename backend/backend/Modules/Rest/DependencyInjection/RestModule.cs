using backend.Modules.Rest.Application;
using backend.Modules.Rest.Infrastructure;

namespace backend.Modules.Rest.DependencyInjection;

public static class RestModule
{
    public static IServiceCollection AddRestModule(this IServiceCollection services)
    {
        services.AddScoped<IRestRepository, RestRepository>();
        services.AddScoped<IRestService, RestService>();

        return services;
    }
}
