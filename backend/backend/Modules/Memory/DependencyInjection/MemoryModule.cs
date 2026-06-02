using backend.Modules.Memory.Application;
using backend.Modules.Memory.Infrastructure;

namespace backend.Modules.Memory.DependencyInjection;

public static class MemoryModule
{
    public static IServiceCollection AddMemoryModule(this IServiceCollection services)
    {
        services.AddScoped<ICampaignMemoryRepository, CampaignMemoryRepository>();
        services.AddScoped<ICampaignMemoryService, CampaignMemoryService>();

        return services;
    }
}
