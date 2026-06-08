using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Modules.AccountCharacters.Contracts;

public sealed class GenerateAccountCharacterRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("species")]
    public string? Species { get; set; }

    [JsonPropertyName("className")]
    public string? ClassName { get; set; }

    [JsonPropertyName("background")]
    public string? Background { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public sealed class UpdateAccountCharacterRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("species")]
    public string? Species { get; set; }

    [JsonPropertyName("className")]
    public string? ClassName { get; set; }

    [JsonPropertyName("background")]
    public string? Background { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("alignment")]
    public string? Alignment { get; set; }

    [JsonPropertyName("attributes")]
    public JsonElement? Attributes { get; set; }

    [JsonPropertyName("resources")]
    public JsonElement? Resources { get; set; }

    [JsonPropertyName("progression")]
    public JsonElement? Progression { get; set; }

    [JsonPropertyName("wealth")]
    public JsonElement? Wealth { get; set; }

    [JsonPropertyName("combat")]
    public JsonElement? Combat { get; set; }

    [JsonPropertyName("inventory")]
    public JsonElement? Inventory { get; set; }

    [JsonPropertyName("equipment")]
    public JsonElement? Equipment { get; set; }

    [JsonPropertyName("attacks")]
    public JsonElement? Attacks { get; set; }

    [JsonPropertyName("metadata")]
    public JsonElement? Metadata { get; set; }
}

public sealed record AccountCharacterDraft(
    string Name,
    string? Species,
    string? ClassName,
    string? Background,
    string? Description,
    string? Alignment,
    JsonElement Attributes,
    JsonElement Resources,
    JsonElement Progression,
    JsonElement Wealth,
    JsonElement Combat,
    JsonElement Inventory,
    JsonElement Equipment,
    JsonElement Attacks,
    JsonElement Metadata);
