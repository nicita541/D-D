using backend.Modules.Economy.Application;
using backend.Modules.Economy.Infrastructure;

namespace backend.Modules.Economy.DependencyInjection;

public static class EconomyModule
{
    public static IServiceCollection AddEconomyModule(this IServiceCollection services)
    {
        services.AddScoped<IEconomyRepository, EconomyRepository>();
        services.AddScoped<IEconomyService, EconomyService>();

        return services;
    }
}
