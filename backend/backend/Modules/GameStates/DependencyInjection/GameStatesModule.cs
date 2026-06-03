using backend.Modules.GameStates.Application;
using backend.Modules.GameStates.Infrastructure;

namespace backend.Modules.GameStates.DependencyInjection;

public static class GameStatesModule
{
    public static IServiceCollection AddGameStatesModule(this IServiceCollection services)
    {
        services.AddScoped<IGameStateRepository, GameStateRepository>();
        services.AddScoped<IGameStateService, GameStateService>();

        return services;
    }
}
