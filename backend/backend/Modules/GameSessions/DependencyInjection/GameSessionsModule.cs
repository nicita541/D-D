using backend.Modules.GameSessions.Application;
using backend.Modules.GameSessions.Infrastructure;

namespace backend.Modules.GameSessions.DependencyInjection;

public static class GameSessionsModule
{
    public static IServiceCollection AddGameSessionsModule(this IServiceCollection services)
    {
        services.AddScoped<IGameSessionRepository, GameSessionRepository>();
        services.AddScoped<IGameSessionService, GameSessionService>();

        return services;
    }
}
