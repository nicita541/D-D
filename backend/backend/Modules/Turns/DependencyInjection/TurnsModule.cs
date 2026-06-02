using backend.Modules.Turns.Application;
using backend.Modules.Turns.Infrastructure;

namespace backend.Modules.Turns.DependencyInjection;

public static class TurnsModule
{
    public static IServiceCollection AddTurnsModule(this IServiceCollection services)
    {
        services.AddScoped<ITurnRepository, TurnRepository>();
        services.AddScoped<ITurnService, TurnService>();

        return services;
    }
}
