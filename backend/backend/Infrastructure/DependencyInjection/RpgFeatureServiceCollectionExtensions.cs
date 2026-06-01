using backend.Repositories.Rpg;
using backend.Modules.Changes;
using backend.Modules.Changes.Handlers;
using backend.Services.Rpg;

namespace backend.Infrastructure.DependencyInjection;

public static class RpgFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddRpgFeatureServices(this IServiceCollection services)
    {
        services.AddScoped<IGameStateRepository, GameStateRepository>();
        services.AddScoped<ICharacterRepository, CharacterRepository>();
        services.AddScoped<ICharacterDomainRepository, CharacterDomainRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IStoryRepository, StoryRepository>();
        services.AddScoped<IPartyRepository, PartyRepository>();
        services.AddScoped<ICombatRepository, CombatRepository>();
        services.AddScoped<IAiMasterContextRepository, AiMasterContextRepository>();
        services.AddScoped<ITurnRepository, TurnRepository>();
        services.AddScoped<IGameChangeRepository, GameChangeRepository>();
        services.AddScoped<IWorldRepository, WorldRepository>();
        services.AddScoped<IDiceRollRepository, DiceRollRepository>();
        services.AddScoped<IAbilityCheckRepository, AbilityCheckRepository>();
        services.AddScoped<ICampaignMemoryRepository, CampaignMemoryRepository>();
        services.AddScoped<IMechanicRequestRepository, MechanicRequestRepository>();
        services.AddScoped<ICharacterProgressionRepository, CharacterProgressionRepository>();
        services.AddScoped<IPlayBootstrapRepository, PlayBootstrapRepository>();
        services.AddScoped<ITravelRepository, TravelRepository>();
        services.AddScoped<GameChangeDispatcher>();
        services.AddScoped<IGameChangeHandler, AddJournalEntryChangeHandler>();
        services.AddScoped<IGameChangeHandler, UpdateMemoryChangeHandler>();
        services.AddScoped<IGameChangeHandler, UpdateSceneChangeHandler>();
        services.AddScoped<IGameChangeHandler, CreateQuestChangeHandler>();
        services.AddScoped<IGameChangeHandler, UpdateQuestChangeHandler>();
        services.AddScoped<IGameChangeHandler, CreateQuestStepChangeHandler>();
        services.AddScoped<IGameChangeHandler, CompleteQuestStepChangeHandler>();
        services.AddScoped<IGameChangeHandler, CreateLocationChangeHandler>();
        services.AddScoped<IGameChangeHandler, UpdateLocationChangeHandler>();
        services.AddScoped<IGameChangeHandler, CreateNpcChangeHandler>();
        services.AddScoped<IGameChangeHandler, UpdateNpcChangeHandler>();
        services.AddScoped<IGameChangeHandler, CreateWorldObjectChangeHandler>();
        services.AddScoped<IGameChangeHandler, UpdateWorldObjectChangeHandler>();
        services.AddScoped<IGameChangeHandler, AddItemChangeHandler>();
        services.AddScoped<IGameChangeHandler, RequestRollChangeHandler>();
        services.AddScoped<IGameChangeHandler, ChangeHpChangeHandler>();
        services.AddScoped<IGameChangeHandler, ChangeResourceChangeHandler>();
        services.AddScoped<IGameChangeHandler, AddConditionChangeHandler>();
        services.AddScoped<IGameChangeHandler, DeleteConditionChangeHandler>();
        services.AddScoped<IGameChangeHandler, MoveItemChangeHandler>();
        services.AddScoped<IGameChangeHandler, MovePartyToLocationChangeHandler>();
        services.AddScoped<IGameChangeHandler, SetCurrentLocationChangeHandler>();
        services.AddScoped<IGameChangeHandler, OpenLocationExitChangeHandler>();
        services.AddScoped<IGameChangeHandler, CloseLocationExitChangeHandler>();
        services.AddScoped<IGameChangeHandler, LockLocationExitChangeHandler>();
        services.AddScoped<IGameChangeHandler, UnlockLocationExitChangeHandler>();

        services.AddScoped<IGameStateService, GameStateService>();
        services.AddScoped<ICharacterService, CharacterService>();
        services.AddScoped<ICharacterDomainService, CharacterDomainService>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IStoryService, StoryService>();
        services.AddScoped<IPartyService, PartyService>();
        services.AddScoped<ICombatService, CombatService>();
        services.AddScoped<IAiMasterContextService, AiMasterContextService>();
        services.AddScoped<ITurnService, TurnService>();
        services.AddScoped<IGameChangeService, GameChangeService>();
        services.AddScoped<IWorldService, WorldService>();
        services.AddScoped<IDiceRollService, DiceRollService>();
        services.AddScoped<IAbilityCheckService, AbilityCheckService>();
        services.AddScoped<ICampaignMemoryService, CampaignMemoryService>();
        services.AddScoped<IMechanicRequestService, MechanicRequestService>();
        services.AddScoped<ICharacterProgressionService, CharacterProgressionService>();
        services.AddScoped<IPlayBootstrapService, PlayBootstrapService>();
        services.AddScoped<IPlayStateService, PlayStateService>();
        services.AddScoped<IPlayOrchestratorService, PlayOrchestratorService>();
        services.AddScoped<ITravelService, TravelService>();
        services.AddScoped<IPromptBuilder, PromptBuilder>();
        services.AddSingleton<IDiceRoller, DiceRoller>();

        return services;
    }
}
