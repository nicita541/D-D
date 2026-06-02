using backend.Modules.Campaigns.Application;
using backend.Modules.Campaigns.Infrastructure;

namespace backend.Modules.Campaigns.DependencyInjection;

public static class CampaignsModule
{
    public static IServiceCollection AddCampaignsModule(this IServiceCollection services)
    {
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<ICampaignService, CampaignService>();

        return services;
    }
}
