using System.Text.Json.Serialization;

namespace backend.Contracts.Rpg.Combat;

public sealed class StartCombatRequest
{
    [JsonPropertyName("участники")]
    public List<AddCombatParticipantRequest> Participants { get; set; } = new();
}

public sealed class AddCombatParticipantRequest
{
    [JsonPropertyName("типАктера")]
    public string ActorType { get; set; } = "character";

    [JsonPropertyName("actorId")]
    public Guid ActorId { get; set; }

    [JsonPropertyName("имя")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("инициатива")]
    public int Initiative { get; set; }

    [JsonPropertyName("хпТекущее")]
    public int HpCurrent { get; set; }

    [JsonPropertyName("хпМаксимум")]
    public int HpMax { get; set; }

    [JsonPropertyName("состояния")]
    public List<string> Conditions { get; set; } = new();

    [JsonPropertyName("классДоспеха")]
    public int? ArmorClassRu { get; set; }

    [JsonPropertyName("armorClass")]
    public int? ArmorClass { get; set; }

    [JsonIgnore]
    public int ResolvedArmorClass => ArmorClassRu ?? ArmorClass ?? 10;
}

public sealed class ApplyCombatDamageRequest
{
    [JsonPropertyName("цельУчастникId")]
    public Guid? TargetParticipantIdRu { get; set; }

    [JsonPropertyName("targetParticipantId")]
    public Guid? TargetParticipantId { get; set; }

    [JsonPropertyName("урон")]
    public int? DamageRu { get; set; }

    [JsonPropertyName("damage")]
    public int? Damage { get; set; }

    [JsonPropertyName("типУрона")]
    public string? DamageTypeRu { get; set; }

    [JsonPropertyName("damageType")]
    public string? DamageType { get; set; }

    [JsonPropertyName("причина")]
    public string? ReasonRu { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonIgnore]
    public Guid? ResolvedTargetParticipantId => TargetParticipantIdRu ?? TargetParticipantId;

    [JsonIgnore]
    public int? ResolvedDamage => DamageRu ?? Damage;

    [JsonIgnore]
    public string ResolvedDamageType => string.IsNullOrWhiteSpace(DamageTypeRu)
        ? DamageType?.Trim() ?? string.Empty
        : DamageTypeRu.Trim();

    [JsonIgnore]
    public string ResolvedReason => string.IsNullOrWhiteSpace(ReasonRu)
        ? Reason?.Trim() ?? string.Empty
        : ReasonRu.Trim();
}

public sealed class HealCombatParticipantRequest
{
    [JsonPropertyName("лечение")]
    public int? HealingRu { get; set; }

    [JsonPropertyName("healing")]
    public int? Healing { get; set; }

    [JsonPropertyName("причина")]
    public string? ReasonRu { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonIgnore]
    public int? ResolvedHealing => HealingRu ?? Healing;

    [JsonIgnore]
    public string ResolvedReason => string.IsNullOrWhiteSpace(ReasonRu)
        ? Reason?.Trim() ?? string.Empty
        : ReasonRu.Trim();
}

public sealed class CombatAttackRequest
{
    [JsonPropertyName("атакующийУчастникId")]
    public Guid? AttackerParticipantIdRu { get; set; }

    [JsonPropertyName("attackerParticipantId")]
    public Guid? AttackerParticipantId { get; set; }

    [JsonPropertyName("цельУчастникId")]
    public Guid? TargetParticipantIdRu { get; set; }

    [JsonPropertyName("targetParticipantId")]
    public Guid? TargetParticipantId { get; set; }

    [JsonPropertyName("бросокАтаки")]
    public string? AttackRollRu { get; set; }

    [JsonPropertyName("attackRoll")]
    public string? AttackRoll { get; set; }

    [JsonPropertyName("урон")]
    public string? DamageRollRu { get; set; }

    [JsonPropertyName("damageRoll")]
    public string? DamageRoll { get; set; }

    [JsonPropertyName("типУрона")]
    public string? DamageTypeRu { get; set; }

    [JsonPropertyName("damageType")]
    public string? DamageType { get; set; }

    [JsonPropertyName("причина")]
    public string? ReasonRu { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonIgnore]
    public Guid? ResolvedAttackerParticipantId => AttackerParticipantIdRu ?? AttackerParticipantId;

    [JsonIgnore]
    public Guid? ResolvedTargetParticipantId => TargetParticipantIdRu ?? TargetParticipantId;

    [JsonIgnore]
    public string ResolvedAttackRoll => string.IsNullOrWhiteSpace(AttackRollRu)
        ? AttackRoll?.Trim() ?? string.Empty
        : AttackRollRu.Trim();

    [JsonIgnore]
    public string ResolvedDamageRoll => string.IsNullOrWhiteSpace(DamageRollRu)
        ? DamageRoll?.Trim() ?? string.Empty
        : DamageRollRu.Trim();

    [JsonIgnore]
    public string ResolvedDamageType => string.IsNullOrWhiteSpace(DamageTypeRu)
        ? DamageType?.Trim() ?? string.Empty
        : DamageTypeRu.Trim();

    [JsonIgnore]
    public string ResolvedReason => string.IsNullOrWhiteSpace(ReasonRu)
        ? Reason?.Trim() ?? string.Empty
        : ReasonRu.Trim();
}
