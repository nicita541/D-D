namespace backend.Modules.Changes;

public sealed class AddJournalEntryChangeHandler() : RepositoryBackedChangeHandlerBase("add_journal_entry", GameChangeOperationClass.Safe, true);

public sealed class UpdateMemoryChangeHandler() : RepositoryBackedChangeHandlerBase("update_memory", GameChangeOperationClass.Safe, true);

public sealed class UpdateSceneChangeHandler() : RepositoryBackedChangeHandlerBase("update_scene", GameChangeOperationClass.Safe, true);

public sealed class CreateQuestChangeHandler() : RepositoryBackedChangeHandlerBase("create_quest", GameChangeOperationClass.Safe, true);

public sealed class UpdateQuestChangeHandler() : RepositoryBackedChangeHandlerBase("update_quest", GameChangeOperationClass.Safe, true);

public sealed class CreateQuestStepChangeHandler() : RepositoryBackedChangeHandlerBase("create_quest_step", GameChangeOperationClass.Safe, true);

public sealed class CompleteQuestStepChangeHandler() : RepositoryBackedChangeHandlerBase("complete_quest_step", GameChangeOperationClass.Safe, true);

public sealed class CreateLocationChangeHandler() : RepositoryBackedChangeHandlerBase("create_location", GameChangeOperationClass.Safe, true);

public sealed class UpdateLocationChangeHandler() : RepositoryBackedChangeHandlerBase("update_location", GameChangeOperationClass.Safe, true);

public sealed class CreateNpcChangeHandler() : RepositoryBackedChangeHandlerBase("create_npc", GameChangeOperationClass.Safe, true);

public sealed class UpdateNpcChangeHandler() : RepositoryBackedChangeHandlerBase("update_npc", GameChangeOperationClass.Safe, true);

public sealed class CreateWorldObjectChangeHandler() : RepositoryBackedChangeHandlerBase("create_world_object", GameChangeOperationClass.Safe, true);

public sealed class UpdateWorldObjectChangeHandler() : RepositoryBackedChangeHandlerBase("update_world_object", GameChangeOperationClass.Safe, true);

public sealed class AddItemChangeHandler() : RepositoryBackedChangeHandlerBase("add_item", GameChangeOperationClass.Safe, true);

public sealed class RequestRollChangeHandler() : RepositoryBackedChangeHandlerBase("request_roll", GameChangeOperationClass.Dangerous, true);

public sealed class ChangeHpChangeHandler() : RepositoryBackedChangeHandlerBase("change_hp", GameChangeOperationClass.Dangerous, true);

public sealed class ChangeResourceChangeHandler() : RepositoryBackedChangeHandlerBase("change_resource", GameChangeOperationClass.Dangerous, true);

public sealed class AddConditionChangeHandler() : RepositoryBackedChangeHandlerBase("add_condition", GameChangeOperationClass.Dangerous, true);

public sealed class DeleteConditionChangeHandler() : RepositoryBackedChangeHandlerBase("delete_condition", GameChangeOperationClass.Dangerous, true);

public sealed class MoveItemChangeHandler() : RepositoryBackedChangeHandlerBase("move_item", GameChangeOperationClass.Dangerous, true);

public sealed class MovePartyToLocationChangeHandler() : RepositoryBackedChangeHandlerBase("move_party_to_location", GameChangeOperationClass.Dangerous, true);

public sealed class SetCurrentLocationChangeHandler() : RepositoryBackedChangeHandlerBase("set_current_location", GameChangeOperationClass.Dangerous, true);

public sealed class OpenLocationExitChangeHandler() : RepositoryBackedChangeHandlerBase("open_location_exit", GameChangeOperationClass.Dangerous, true);

public sealed class CloseLocationExitChangeHandler() : RepositoryBackedChangeHandlerBase("close_location_exit", GameChangeOperationClass.Dangerous, true);

public sealed class LockLocationExitChangeHandler() : RepositoryBackedChangeHandlerBase("lock_location_exit", GameChangeOperationClass.Dangerous, true);

public sealed class UnlockLocationExitChangeHandler() : RepositoryBackedChangeHandlerBase("unlock_location_exit", GameChangeOperationClass.Dangerous, true);
