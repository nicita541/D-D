using backend.Modules.Travel.Application;
using backend.Modules.Travel.Infrastructure;

namespace backend.Modules.Travel.DependencyInjection;

public static class TravelModule
{
    public static IServiceCollection AddTravelModule(this IServiceCollection services)
    {
        services.AddScoped<ITravelRepository, TravelRepository>();
        services.AddScoped<ITravelService, TravelService>();

        return services;
    }
}
