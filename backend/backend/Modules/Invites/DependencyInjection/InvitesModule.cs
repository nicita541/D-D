using backend.Modules.Invites.Application;
using backend.Modules.Invites.Infrastructure;

namespace backend.Modules.Invites.DependencyInjection;

public static class InvitesModule
{
    public static IServiceCollection AddInvitesModule(this IServiceCollection services)
    {
        services.AddScoped<IInviteRepository, InviteRepository>();
        services.AddScoped<IInviteService, InviteService>();

        return services;
    }
}
