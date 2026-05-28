using System.Text.Json.Serialization;

namespace backend.Models;

public class Resources
{
    [JsonPropertyName("хп")]
    public Stat Hp { get; set; } = new();

    [JsonPropertyName("мана")]
    public Stat Mana { get; set; } = new();

    [JsonPropertyName("очкидействий")]
    public Stat ActionPoints { get; set; } = new();

    [JsonPropertyName("состояния")]
    public List<Condition> Conditions { get; set; } = new();

    [JsonPropertyName("ресурсыспособностей")]
    public List<LimitedResource> LimitedResources { get; set; } = new();

    [JsonPropertyName("спасброскисмерти")]
    public DeathSaves DeathSaves { get; set; } = new();
}

public class Stat
{
    [JsonPropertyName("максимум")]
    public int Max { get; set; }

    [JsonPropertyName("текущее")]
    public int Current { get; set; }
}

public class LimitedResource
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = $"resource_{Guid.NewGuid():N}";

    [JsonPropertyName("название")]
    public string Name { get; set; } = "";

    [JsonPropertyName("максимум")]
    public int Max { get; set; }

    [JsonPropertyName("текущее")]
    public int Current { get; set; }

    [JsonPropertyName("восстановление")]
    public string Recovery { get; set; } = "короткий_отдых";
}

public class Condition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = $"condition_{Guid.NewGuid():N}";

    [JsonPropertyName("название")]
    public string Name { get; set; } = "";

    [JsonPropertyName("тип")]
    public string Type { get; set; } = ""; // яд, болезнь, травма, бафф, дебафф, контроль, магия

    [JsonPropertyName("описание")]
    public string Description { get; set; } = "";

    [JsonPropertyName("источник")]
    public string? Source { get; set; }

    [JsonPropertyName("оставшиесяходы")]
    public int? RemainingTurns { get; set; }

    [JsonPropertyName("постоянное")]
    public bool IsPermanent { get; set; }

    [JsonPropertyName("стаки")]
    public int Stacks { get; set; } = 1;

    [JsonPropertyName("максимумстаков")]
    public int? MaxStacks { get; set; }

    [JsonPropertyName("эффекты")]
    public List<EffectModifier> Effects { get; set; } = new();

    [JsonPropertyName("теги")]
    public List<string> Tags { get; set; } = new();
}

public class EffectModifier
{
    [JsonPropertyName("цель")]
    public string Target { get; set; } = "";

    [JsonPropertyName("операция")]
    public string Operation { get; set; } = ""; // добавить, убрать, установить, преимущество, помеха

    [JsonPropertyName("значение")]
    public int? Value { get; set; }

    [JsonPropertyName("кубик")]
    public string? Dice { get; set; }

    [JsonPropertyName("причина")]
    public string? Reason { get; set; }
}

public class DeathSaves
{
    [JsonPropertyName("активны")]
    public bool IsActive { get; set; }

    [JsonPropertyName("успехи")]
    public int Successes { get; set; }

    [JsonPropertyName("провалы")]
    public int Failures { get; set; }
}
