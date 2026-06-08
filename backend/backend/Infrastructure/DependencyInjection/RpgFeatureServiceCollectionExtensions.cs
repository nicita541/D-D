using backend.Modules.Ai.DependencyInjection;
using backend.Modules.Campaigns.DependencyInjection;
using backend.Modules.Characters.DependencyInjection;
using backend.Modules.Changes.DependencyInjection;
using backend.Modules.Combat.DependencyInjection;
using backend.Modules.GameStates.DependencyInjection;
using backend.Modules.Invites.DependencyInjection;
using backend.Modules.Mechanics.DependencyInjection;
using backend.Modules.Memory.DependencyInjection;
using backend.Modules.Party.DependencyInjection;
using backend.Modules.Play.DependencyInjection;
using backend.Modules.Snapshots.DependencyInjection;
using backend.Modules.Story.DependencyInjection;
using backend.Modules.Travel.DependencyInjection;
using backend.Modules.Turns.DependencyInjection;
using backend.Modules.World.DependencyInjection;

namespace backend.Infrastructure.DependencyInjection;

public static class RpgFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddRpgFeatureServices(this IServiceCollection services)
    {
        services.AddGameStatesModule();
        services.AddInvitesModule();
        services.AddCharactersModule();
        services.AddWorldModule();
        services.AddTravelModule();
        services.AddCombatModule();
        services.AddChangesModule();
        services.AddTurnsModule();
        services.AddMechanicsModule();
        services.AddMemoryModule();
        services.AddAiModule();
        services.AddCampaignsModule();
        services.AddPartyModule();
        services.AddStoryModule();
        services.AddSnapshotsModule();
        services.AddPlayModule();

        return services;
    }
}
