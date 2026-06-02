using backend.Modules.Combat.Application;
using backend.Modules.Combat.Infrastructure;

namespace backend.Modules.Combat.DependencyInjection;

public static class CombatModule
{
    public static IServiceCollection AddCombatModule(this IServiceCollection services)
    {
        services.AddScoped<ICombatRepository, CombatRepository>();
        services.AddScoped<ICombatService, CombatService>();

        return services;
    }
}
