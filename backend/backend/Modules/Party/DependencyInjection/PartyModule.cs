using backend.Modules.Party.Application;
using backend.Modules.Party.Infrastructure;

namespace backend.Modules.Party.DependencyInjection;

public static class PartyModule
{
    public static IServiceCollection AddPartyModule(this IServiceCollection services)
    {
        services.AddScoped<IPartyRepository, PartyRepository>();
        services.AddScoped<IPartyService, PartyService>();

        return services;
    }
}
