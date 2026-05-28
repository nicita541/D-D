using backend.Repositories.Rpg;
using backend.Services.Rpg;

namespace backend.Infrastructure.DependencyInjection;

public static class RpgFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddRpgFeatureServices(this IServiceCollection services)
    {
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IStoryRepository, StoryRepository>();
        services.AddScoped<IPartyRepository, PartyRepository>();
        services.AddScoped<ICombatRepository, CombatRepository>();
        services.AddScoped<IAiMasterContextRepository, AiMasterContextRepository>();

        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IStoryService, StoryService>();
        services.AddScoped<IPartyService, PartyService>();
        services.AddScoped<ICombatService, CombatService>();
        services.AddScoped<IAiMasterContextService, AiMasterContextService>();

        return services;
    }
}
