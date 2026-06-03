using backend.Modules.World.Application;
using backend.Modules.World.Infrastructure;

namespace backend.Modules.World.DependencyInjection;

public static class WorldModule
{
    public static IServiceCollection AddWorldModule(this IServiceCollection services)
    {
        services.AddScoped<IWorldRepository, WorldRepository>();
        services.AddScoped<IWorldService, WorldService>();

        return services;
    }
}
