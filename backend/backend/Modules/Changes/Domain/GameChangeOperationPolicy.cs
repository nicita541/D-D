namespace backend.Modules.Changes.Domain;

public enum GameChangeOperationClass
{
    Unknown,
    Safe,
    Dangerous,
    Unsupported
}

public sealed record GameChangeOperationDescriptor(
    string OriginalOperation,
    string CanonicalOperation,
    GameChangeOperationClass Class,
    bool IsSupported)
{
    public bool IsKnown => Class != GameChangeOperationClass.Unknown;

    public bool IsSafeAutoApply => Class == GameChangeOperationClass.Safe && IsSupported;
}

public static class GameChangeOperationPolicy
{
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["add_journal_entry"] = "add_journal_entry",
        ["добавить_запись_журнала"] = "add_journal_entry",
        ["добавить_заметку_журнала"] = "add_journal_entry",

        ["update_memory"] = "update_memory",
        ["обновить_память"] = "update_memory",

        ["update_scene"] = "update_scene",
        ["обновить_сцену"] = "update_scene",

        ["create_quest"] = "create_quest",
        ["создать_квест"] = "create_quest",

        ["update_quest"] = "update_quest",
        ["обновить_квест"] = "update_quest",

        ["create_quest_step"] = "create_quest_step",
        ["создать_шаг_квеста"] = "create_quest_step",

        ["complete_quest_step"] = "complete_quest_step",
        ["завершить_шаг_квеста"] = "complete_quest_step",

        ["complete_quest"] = "complete_quest",
        ["завершить_квест"] = "complete_quest",

        ["fail_quest"] = "fail_quest",
        ["провалить_квест"] = "fail_quest",

        ["create_location"] = "create_location",
        ["создать_локацию"] = "create_location",

        ["update_location"] = "update_location",
        ["обновить_локацию"] = "update_location",

        ["create_location_exit"] = "create_location_exit",
        ["создать_выход_локации"] = "create_location_exit",

        ["update_location_exit"] = "update_location_exit",
        ["обновить_выход_локации"] = "update_location_exit",

        ["open_location_exit"] = "open_location_exit",
        ["открыть_выход_локации"] = "open_location_exit",

        ["close_location_exit"] = "close_location_exit",
        ["закрыть_выход_локации"] = "close_location_exit",

        ["lock_location_exit"] = "lock_location_exit",
        ["запереть_выход_локации"] = "lock_location_exit",

        ["unlock_location_exit"] = "unlock_location_exit",
        ["отпереть_выход_локации"] = "unlock_location_exit",

        ["move_party_to_location"] = "move_party_to_location",
        ["переместить_партию_в_локацию"] = "move_party_to_location",

        ["set_current_location"] = "set_current_location",
        ["установить_текущую_локацию"] = "set_current_location",

        ["create_npc"] = "create_npc",
        ["создать_npc"] = "create_npc",

        ["update_npc"] = "update_npc",
        ["обновить_npc"] = "update_npc",

        ["create_world_object"] = "create_world_object",
        ["создать_объект"] = "create_world_object",

        ["update_world_object"] = "update_world_object",
        ["обновить_объект"] = "update_world_object",

        ["create_monster"] = "create_monster",
        ["создать_монстра"] = "create_monster",
        ["создать_монстра"] = "create_monster",

        ["spawn_monster"] = "spawn_monster",
        ["заспавнить_монстра"] = "spawn_monster",
        ["заспавнить_монстра"] = "spawn_monster",

        ["kill_monster"] = "kill_monster",
        ["убить_монстра"] = "kill_monster",
        ["убить_монстра"] = "kill_monster",

        ["add_item"] = "add_item",
        ["добавить_предмет"] = "add_item",

        ["remove_item"] = "remove_item",
        ["удалить_предмет"] = "remove_item",

        ["equip_item"] = "equip_item",
        ["экипировать_предмет"] = "equip_item",

        ["unequip_item"] = "unequip_item",
        ["снять_предмет"] = "unequip_item",

        ["use_item"] = "use_item",
        ["использовать_предмет"] = "use_item",

        ["transfer_item"] = "transfer_item",
        ["передать_предмет"] = "transfer_item",

        ["take_item"] = "take_item",
        ["взять_предмет"] = "take_item",

        ["drop_item"] = "drop_item",
        ["бросить_предмет"] = "drop_item",

        ["loot_container"] = "loot_container",
        ["обыскать_контейнер"] = "loot_container",

        ["move_item"] = "move_item",
        ["переместить_предмет"] = "move_item",

        ["request_roll"] = "request_roll",
        ["запросить_бросок"] = "request_roll",

        ["change_hp"] = "change_hp",
        ["изменить_хп"] = "change_hp",

        ["change_resource"] = "change_resource",
        ["change_mana"] = "change_resource",
        ["изменить_ресурс"] = "change_resource",

        ["add_condition"] = "add_condition",
        ["добавить_состояние"] = "add_condition",

        ["delete_condition"] = "delete_condition",
        ["удалить_состояние"] = "delete_condition",

        ["apply_condition_duration"] = "apply_condition_duration",
        ["применить_длительность_состояния"] = "apply_condition_duration",

        ["tick_conditions"] = "tick_conditions",
        ["тик_состояний"] = "tick_conditions",

        ["move_character"] = "move_character",
        ["переместить_персонажа"] = "move_character",

        ["start_combat"] = "start_combat",
        ["начать_бой"] = "start_combat",

        ["end_combat"] = "end_combat",
        ["закончить_бой"] = "end_combat",

        ["add_combat_participant"] = "add_combat_participant",
        ["добавить_участника_боя"] = "add_combat_participant",

        ["remove_combat_participant"] = "remove_combat_participant",
        ["удалить_участника_боя"] = "remove_combat_participant",

        ["add_xp"] = "add_xp",
        ["добавить_опыт"] = "add_xp",
        ["добавить_опыт"] = "add_xp",

        ["level_up"] = "level_up",
        ["повысить_уровень"] = "level_up",

        ["grant_reward"] = "grant_reward",
        ["выдать_награду"] = "grant_reward",
        ["выдать_награду"] = "grant_reward",

        ["grant_quest_reward"] = "grant_quest_reward",
        ["выдать_награду_квеста"] = "grant_quest_reward",
        ["выдать_награду_квеста"] = "grant_quest_reward",

        ["add_currency"] = "add_currency",
        ["добавить_валюту"] = "add_currency",
        ["добавить_валюту"] = "add_currency",

        ["spend_currency"] = "spend_currency",
        ["потратить_валюту"] = "spend_currency",
        ["потратить_валюту"] = "spend_currency",

        ["transfer_currency"] = "transfer_currency",
        ["передать_валюту"] = "transfer_currency",
        ["передать_валюту"] = "transfer_currency",

        ["short_rest"] = "short_rest",
        ["короткий_отдых"] = "short_rest",

        ["long_rest"] = "long_rest",
        ["долгий_отдых"] = "long_rest",

        ["advance_time"] = "advance_time",
        ["продвинуть_время"] = "advance_time",

        ["kill_character"] = "kill_character",
        ["убить_персонажа"] = "kill_character"
    };

    private static readonly HashSet<string> SafeOperations = new(StringComparer.OrdinalIgnoreCase)
    {
        "add_journal_entry",
        "update_memory",
        "update_scene",
        "create_quest",
        "update_quest",
        "create_quest_step",
        "complete_quest_step",
        "create_location",
        "update_location",
        "create_npc",
        "update_npc",
        "create_world_object",
        "update_world_object",
        "add_item"
    };

    private static readonly HashSet<string> DangerousOperations = new(StringComparer.OrdinalIgnoreCase)
    {
        "request_roll",
        "move_party_to_location",
        "set_current_location",
        "open_location_exit",
        "close_location_exit",
        "lock_location_exit",
        "unlock_location_exit",
        "move_character",
        "change_hp",
        "change_resource",
        "change_mana",
        "add_xp",
        "level_up",
        "grant_reward",
        "grant_quest_reward",
        "add_currency",
        "spend_currency",
        "transfer_currency",
        "remove_item",
        "equip_item",
        "unequip_item",
        "use_item",
        "transfer_item",
        "take_item",
        "drop_item",
        "loot_container",
        "move_item",
        "start_combat",
        "end_combat",
        "add_combat_participant",
        "remove_combat_participant",
        "short_rest",
        "long_rest",
        "advance_time",
        "apply_condition_duration",
        "tick_conditions",
        "add_condition",
        "delete_condition",
        "create_monster",
        "spawn_monster",
        "kill_monster",
        "add_xp",
        "complete_quest",
        "grant_reward",
        "grant_quest_reward",
        "add_currency",
        "spend_currency",
        "kill_character"
    };

    private static readonly HashSet<string> SupportedOperations = new(StringComparer.OrdinalIgnoreCase)
    {
        "add_journal_entry",
        "update_memory",
        "update_scene",
        "create_quest",
        "update_quest",
        "create_quest_step",
        "complete_quest_step",
        "create_location",
        "update_location",
        "create_npc",
        "update_npc",
        "create_world_object",
        "update_world_object",
        "add_item",
        "request_roll",
        "change_hp",
        "change_resource",
        "add_condition",
        "delete_condition",
        "move_item",
        "move_party_to_location",
        "set_current_location",
        "open_location_exit",
        "close_location_exit",
        "lock_location_exit",
        "unlock_location_exit",
        "create_monster",
        "spawn_monster",
        "kill_monster",
        "add_xp",
        "grant_reward",
        "grant_quest_reward",
        "add_currency",
        "spend_currency"
    };

    public static IReadOnlyCollection<string> AllowedAiOperations => Aliases.Keys.ToArray();

    public static GameChangeOperationDescriptor Describe(string? operation)
    {
        var original = operation?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(original) || !Aliases.TryGetValue(original, out var canonical))
        {
            return new GameChangeOperationDescriptor(original, original, GameChangeOperationClass.Unknown, false);
        }

        var operationClass = SafeOperations.Contains(canonical)
            ? GameChangeOperationClass.Safe
            : DangerousOperations.Contains(canonical)
                ? GameChangeOperationClass.Dangerous
                : GameChangeOperationClass.Unsupported;

        return new GameChangeOperationDescriptor(original, canonical, operationClass, SupportedOperations.Contains(canonical));
    }

    public static bool IsKnown(string? operation) => Describe(operation).IsKnown;

    public static bool IsSupported(string? operation) => Describe(operation).IsSupported;

    public static bool IsSafeAutoApply(string? operation) => Describe(operation).IsSafeAutoApply;

    public static string? TryCanonicalize(string? operation)
    {
        var descriptor = Describe(operation);
        return descriptor.IsKnown ? descriptor.CanonicalOperation : null;
    }
}
