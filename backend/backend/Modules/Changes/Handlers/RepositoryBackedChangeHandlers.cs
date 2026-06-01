using System.Text.Json;

namespace backend.Modules.Changes.Handlers;

public abstract class RepositoryBackedChangeHandler : IGameChangeHandler
{
    protected RepositoryBackedChangeHandler(string operation, GameChangeOperationClass operationClass, bool isSupported)
    {
        Operation = operation;
        Class = operationClass;
        IsSupported = isSupported;
    }

    public string Operation { get; }

    public GameChangeOperationClass Class { get; }

    public bool IsSupported { get; }

    public Task<JsonElement> ApplyAsync(
        GameChangeContext context,
        JsonElement payload,
        CancellationToken cancellationToken)
        => context.ApplyCanonicalOperationAsync(Operation, payload, cancellationToken);
}

public sealed class AddJournalEntryChangeHandler() : RepositoryBackedChangeHandler("add_journal_entry", GameChangeOperationClass.Safe, true);

public sealed class UpdateMemoryChangeHandler() : RepositoryBackedChangeHandler("update_memory", GameChangeOperationClass.Safe, true);

public sealed class UpdateSceneChangeHandler() : RepositoryBackedChangeHandler("update_scene", GameChangeOperationClass.Safe, true);

public sealed class CreateQuestChangeHandler() : RepositoryBackedChangeHandler("create_quest", GameChangeOperationClass.Safe, true);

public sealed class UpdateQuestChangeHandler() : RepositoryBackedChangeHandler("update_quest", GameChangeOperationClass.Safe, true);

public sealed class CreateQuestStepChangeHandler() : RepositoryBackedChangeHandler("create_quest_step", GameChangeOperationClass.Safe, true);

public sealed class CompleteQuestStepChangeHandler() : RepositoryBackedChangeHandler("complete_quest_step", GameChangeOperationClass.Safe, true);

public sealed class CreateLocationChangeHandler() : RepositoryBackedChangeHandler("create_location", GameChangeOperationClass.Safe, true);

public sealed class UpdateLocationChangeHandler() : RepositoryBackedChangeHandler("update_location", GameChangeOperationClass.Safe, true);

public sealed class CreateNpcChangeHandler() : RepositoryBackedChangeHandler("create_npc", GameChangeOperationClass.Safe, true);

public sealed class UpdateNpcChangeHandler() : RepositoryBackedChangeHandler("update_npc", GameChangeOperationClass.Safe, true);

public sealed class CreateWorldObjectChangeHandler() : RepositoryBackedChangeHandler("create_world_object", GameChangeOperationClass.Safe, true);

public sealed class UpdateWorldObjectChangeHandler() : RepositoryBackedChangeHandler("update_world_object", GameChangeOperationClass.Safe, true);

public sealed class AddItemChangeHandler() : RepositoryBackedChangeHandler("add_item", GameChangeOperationClass.Safe, true);

public sealed class RequestRollChangeHandler() : RepositoryBackedChangeHandler("request_roll", GameChangeOperationClass.Dangerous, true);

public sealed class ChangeHpChangeHandler() : RepositoryBackedChangeHandler("change_hp", GameChangeOperationClass.Dangerous, true);

public sealed class ChangeResourceChangeHandler() : RepositoryBackedChangeHandler("change_resource", GameChangeOperationClass.Dangerous, true);

public sealed class AddConditionChangeHandler() : RepositoryBackedChangeHandler("add_condition", GameChangeOperationClass.Dangerous, true);

public sealed class DeleteConditionChangeHandler() : RepositoryBackedChangeHandler("delete_condition", GameChangeOperationClass.Dangerous, true);

public sealed class MoveItemChangeHandler() : RepositoryBackedChangeHandler("move_item", GameChangeOperationClass.Dangerous, true);

public sealed class MovePartyToLocationChangeHandler() : RepositoryBackedChangeHandler("move_party_to_location", GameChangeOperationClass.Dangerous, true);

public sealed class SetCurrentLocationChangeHandler() : RepositoryBackedChangeHandler("set_current_location", GameChangeOperationClass.Dangerous, true);

public sealed class OpenLocationExitChangeHandler() : RepositoryBackedChangeHandler("open_location_exit", GameChangeOperationClass.Dangerous, true);

public sealed class CloseLocationExitChangeHandler() : RepositoryBackedChangeHandler("close_location_exit", GameChangeOperationClass.Dangerous, true);

public sealed class LockLocationExitChangeHandler() : RepositoryBackedChangeHandler("lock_location_exit", GameChangeOperationClass.Dangerous, true);

public sealed class UnlockLocationExitChangeHandler() : RepositoryBackedChangeHandler("unlock_location_exit", GameChangeOperationClass.Dangerous, true);
