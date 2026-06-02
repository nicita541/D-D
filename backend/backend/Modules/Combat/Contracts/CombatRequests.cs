using System.Text.Json.Serialization;

namespace backend.Modules.Combat.Contracts;

public sealed class StartCombatRequest
{
    [JsonPropertyName("участники")]
    public List<AddCombatParticipantRequest> Participants { get; set; } = new();

    [JsonPropertyName("participants")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<AddCombatParticipantRequest>? ParticipantsAlias
    {
        get => null;
        set
        {
            if (value is not null)
            {
                Participants = value;
            }
        }
    }
}

public sealed class AddCombatParticipantRequest
{
    [JsonPropertyName("типАктера")]
    public string ActorType { get; set; } = "character";

    [JsonPropertyName("actorType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ActorTypeAlias
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                ActorType = value;
            }
        }
    }

    [JsonPropertyName("actorId")]
    public Guid ActorId { get; set; }

    [JsonPropertyName("актерId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ActorIdRu
    {
        get => null;
        set
        {
            if (value.HasValue)
            {
                ActorId = value.Value;
            }
        }
    }

    [JsonPropertyName("имя")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NameAlias
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Name = value;
            }
        }
    }

    [JsonPropertyName("инициатива")]
    public int Initiative { get; set; }

    [JsonPropertyName("initiative")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? InitiativeAlias
    {
        get => null;
        set
        {
            if (value.HasValue)
            {
                Initiative = value.Value;
            }
        }
    }

    [JsonPropertyName("хпТекущее")]
    public int HpCurrent { get; set; }

    [JsonPropertyName("hpCurrent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? HpCurrentAlias
    {
        get => null;
        set
        {
            if (value.HasValue)
            {
                HpCurrent = value.Value;
            }
        }
    }

    [JsonPropertyName("хпМаксимум")]
    public int HpMax { get; set; }

    [JsonPropertyName("hpMax")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? HpMaxAlias
    {
        get => null;
        set
        {
            if (value.HasValue)
            {
                HpMax = value.Value;
            }
        }
    }

    [JsonPropertyName("состояния")]
    public List<string> Conditions { get; set; } = new();

    [JsonPropertyName("conditions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ConditionsAlias
    {
        get => null;
        set
        {
            if (value is not null)
            {
                Conditions = value;
            }
        }
    }

    [JsonPropertyName("классДоспеха")]
    public int? ArmorClassRu { get; set; }

    [JsonPropertyName("armorClass")]
    public int? ArmorClass { get; set; }

    [JsonIgnore]
    public int ResolvedArmorClass => ArmorClassRu ?? ArmorClass ?? 10;

    [JsonPropertyName("monsterId")]
    public Guid? MonsterId { get; set; }

    [JsonPropertyName("монстрId")]
    public Guid? MonsterIdRu { get; set; }

    [JsonPropertyName("isEnemy")]
    public bool? IsEnemy { get; set; }

    [JsonPropertyName("враг")]
    public bool? IsEnemyRu { get; set; }

    [JsonPropertyName("xpReward")]
    public int? XpReward { get; set; }

    [JsonPropertyName("опытНаграда")]
    public int? XpRewardRu { get; set; }

    [JsonPropertyName("currencyReward")]
    public int? CurrencyReward { get; set; }

    [JsonPropertyName("золотоНаграда")]
    public int? CurrencyRewardRu { get; set; }

    [JsonIgnore]
    public Guid ResolvedActorId => MonsterIdRu ?? MonsterId ?? ActorId;

    [JsonIgnore]
    public string ResolvedActorType => (MonsterIdRu ?? MonsterId).HasValue ? "monster" : ActorType;

    [JsonIgnore]
    public bool? ResolvedIsEnemy => IsEnemyRu ?? IsEnemy;

    [JsonIgnore]
    public int ResolvedXpReward => Math.Max(0, XpRewardRu ?? XpReward ?? 0);

    [JsonIgnore]
    public int ResolvedCurrencyReward => Math.Max(0, CurrencyRewardRu ?? CurrencyReward ?? 0);
}

public sealed class PlayCombatResolveOutcomeRequest
{
    [JsonPropertyName("autoGrantRewards")]
    public bool? AutoGrantRewards { get; set; }

    [JsonPropertyName("выдатьНаграды")]
    public bool? AutoGrantRewardsRu { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("заметка")]
    public string? NoteRu { get; set; }

    [JsonIgnore]
    public bool ResolvedAutoGrantRewards => AutoGrantRewardsRu ?? AutoGrantRewards ?? true;

    [JsonIgnore]
    public string ResolvedNote => string.IsNullOrWhiteSpace(NoteRu)
        ? Note?.Trim() ?? string.Empty
        : NoteRu.Trim();
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
