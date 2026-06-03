using backend.Modules.Mechanics.Application;
using backend.Modules.Mechanics.Infrastructure;

namespace backend.Modules.Mechanics.DependencyInjection;

public static class MechanicsModule
{
    public static IServiceCollection AddMechanicsModule(this IServiceCollection services)
    {
        services.AddScoped<IDiceRollRepository, DiceRollRepository>();
        services.AddScoped<IAbilityCheckRepository, AbilityCheckRepository>();
        services.AddScoped<IMechanicRequestRepository, MechanicRequestRepository>();
        services.AddScoped<IDiceRollService, DiceRollService>();
        services.AddScoped<IAbilityCheckService, AbilityCheckService>();
        services.AddScoped<IMechanicRequestService, MechanicRequestService>();
        services.AddSingleton<IDiceRoller, DiceRoller>();

        return services;
    }
}
