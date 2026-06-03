using backend.Modules.Conditions.Application;
using backend.Modules.Conditions.Infrastructure;

namespace backend.Modules.Conditions.DependencyInjection;

public static class ConditionsModule
{
    public static IServiceCollection AddConditionsModule(this IServiceCollection services)
    {
        services.AddScoped<IConditionStateRepository, ConditionStateRepository>();
        services.AddScoped<IConditionStateService, ConditionStateService>();

        return services;
    }
}
