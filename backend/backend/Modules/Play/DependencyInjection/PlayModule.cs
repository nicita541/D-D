using backend.Modules.Play.Application;
using backend.Modules.Play.Infrastructure;

namespace backend.Modules.Play.DependencyInjection;

public static class PlayModule
{
    public static IServiceCollection AddPlayModule(this IServiceCollection services)
    {
        services.AddScoped<IPlayBootstrapRepository, PlayBootstrapRepository>();
        services.AddScoped<IPlayBootstrapService, PlayBootstrapService>();
        services.AddScoped<IPlayStateService, PlayStateService>();
        services.AddScoped<IPlayOrchestratorService, PlayOrchestratorService>();
        services.AddScoped<IPlayApplicationService, PlayApplicationService>();
        services.AddScoped<IPlayTravelFacade, PlayTravelFacade>();
        services.AddScoped<IPlayCombatFacade, PlayCombatFacade>();

        return services;
    }
}
