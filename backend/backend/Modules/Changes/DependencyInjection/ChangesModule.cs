using backend.Modules.Changes.Application;
using backend.Modules.Changes.Infrastructure;

namespace backend.Modules.Changes.DependencyInjection;

public static class ChangesModule
{
    public static IServiceCollection AddChangesModule(this IServiceCollection services)
    {
        services.AddScoped<IGameChangeRepository, GameChangeRepository>();
        services.AddScoped<IGameChangeService, GameChangeService>();
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

        return services;
    }
}
