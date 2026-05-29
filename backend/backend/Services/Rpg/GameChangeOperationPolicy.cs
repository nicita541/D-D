namespace backend.Services.Rpg;

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

        ["create_location"] = "create_location",
        ["создать_локацию"] = "create_location",

        ["update_location"] = "update_location",
        ["обновить_локацию"] = "update_location",

        ["create_npc"] = "create_npc",
        ["создать_npc"] = "create_npc",

        ["update_npc"] = "update_npc",
        ["обновить_npc"] = "update_npc",

        ["create_world_object"] = "create_world_object",
        ["создать_объект"] = "create_world_object",

        ["update_world_object"] = "update_world_object",
        ["обновить_объект"] = "update_world_object",

        ["add_item"] = "add_item",
        ["добавить_предмет"] = "add_item",

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

        ["move_item"] = "move_item",
        ["move_character"] = "move_character",
        ["переместить_предмет"] = "move_item",

        ["remove_item"] = "remove_item",
        ["удалить_предмет"] = "remove_item",

        ["equip_item"] = "equip_item",
        ["экипировать_предмет"] = "equip_item",

        ["unequip_item"] = "unequip_item",
        ["снять_предмет"] = "unequip_item",

        ["add_xp"] = "add_xp",
        ["add_currency"] = "add_currency",
        ["spend_currency"] = "spend_currency",
        ["start_combat"] = "start_combat",
        ["end_combat"] = "end_combat",
        ["kill_character"] = "kill_character"
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
        "move_character",
        "change_hp",
        "change_resource",
        "change_mana",
        "add_xp",
        "add_currency",
        "spend_currency",
        "remove_item",
        "equip_item",
        "unequip_item",
        "move_item",
        "start_combat",
        "end_combat",
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
        "move_item"
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
}
