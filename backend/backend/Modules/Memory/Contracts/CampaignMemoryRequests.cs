using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Modules.Memory.Contracts;

public sealed class CampaignMemoryRequest
{
    [JsonPropertyName("резюме")]
    public string? SummaryRu { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("текущаяСцена")]
    public JsonElement? CurrentSceneRu { get; set; }

    [JsonPropertyName("currentScene")]
    public JsonElement? CurrentScene { get; set; }

    [JsonPropertyName("важныеФакты")]
    public JsonElement? ImportantFactsRu { get; set; }

    [JsonPropertyName("importantFacts")]
    public JsonElement? ImportantFacts { get; set; }

    [JsonPropertyName("открытыеЛинии")]
    public JsonElement? OpenThreadsRu { get; set; }

    [JsonPropertyName("openThreads")]
    public JsonElement? OpenThreads { get; set; }

    [JsonPropertyName("закрытыеЛинии")]
    public JsonElement? ResolvedThreadsRu { get; set; }

    [JsonPropertyName("resolvedThreads")]
    public JsonElement? ResolvedThreads { get; set; }

    [JsonPropertyName("известныеNpc")]
    public JsonElement? KnownNpcsRu { get; set; }

    [JsonPropertyName("knownNpcs")]
    public JsonElement? KnownNpcs { get; set; }

    [JsonPropertyName("известныеЛокации")]
    public JsonElement? KnownLocationsRu { get; set; }

    [JsonPropertyName("knownLocations")]
    public JsonElement? KnownLocations { get; set; }

    [JsonPropertyName("секретыМастера")]
    public JsonElement? MasterSecretsRu { get; set; }

    [JsonPropertyName("masterSecrets")]
    public JsonElement? MasterSecrets { get; set; }

    [JsonIgnore]
    public string? ResolvedSummary => SummaryRu ?? Summary;

    [JsonIgnore]
    public JsonElement? ResolvedCurrentScene => CurrentSceneRu ?? CurrentScene;

    [JsonIgnore]
    public JsonElement? ResolvedImportantFacts => ImportantFactsRu ?? ImportantFacts;

    [JsonIgnore]
    public JsonElement? ResolvedOpenThreads => OpenThreadsRu ?? OpenThreads;

    [JsonIgnore]
    public JsonElement? ResolvedResolvedThreads => ResolvedThreadsRu ?? ResolvedThreads;

    [JsonIgnore]
    public JsonElement? ResolvedKnownNpcs => KnownNpcsRu ?? KnownNpcs;

    [JsonIgnore]
    public JsonElement? ResolvedKnownLocations => KnownLocationsRu ?? KnownLocations;

    [JsonIgnore]
    public JsonElement? ResolvedMasterSecrets => MasterSecretsRu ?? MasterSecrets;
}

public sealed class CampaignMemorySummarizeRequest
{
    [JsonPropertyName("последниеЗаписи")]
    public int? RecentEntriesRu { get; set; }

    [JsonPropertyName("recentEntries")]
    public int? RecentEntries { get; set; }

    [JsonIgnore]
    public int ResolvedRecentEntries => RecentEntriesRu ?? RecentEntries ?? 20;
}
