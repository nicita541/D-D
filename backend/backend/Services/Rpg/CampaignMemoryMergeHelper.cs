using System.Text.Json;
using backend.Contracts.Rpg.Common;

namespace backend.Services.Rpg;

public sealed record CampaignMemoryMergedPatch(
    string Summary,
    JsonElement CurrentScene,
    JsonElement ImportantFacts,
    JsonElement OpenThreads,
    JsonElement ResolvedThreads,
    JsonElement KnownNpcs,
    JsonElement KnownLocations,
    JsonElement MasterSecrets,
    IReadOnlyList<string> UpdatedFields);

public static class CampaignMemoryMergeHelper
{
    public static CampaignMemoryMergedPatch Merge(JsonElement currentMemory, JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new RpgValidationException("payload обновить_память должен быть JSON object.");
        }

        if (!payload.EnumerateObject().Any())
        {
            throw new RpgValidationException("payload обновить_память не должен быть пустым.");
        }

        var updated = new List<string>();
        var summary = GetOptionalString(currentMemory, "резюме", "summary") ?? string.Empty;
        var currentScene = GetObjectOrDefault(currentMemory, "текущаяСцена", "currentScene");
        var importantFacts = GetArrayOrDefault(currentMemory, "важныеФакты", "importantFacts");
        var openThreads = GetArrayOrDefault(currentMemory, "открытыеЛинии", "openThreads");
        var resolvedThreads = GetArrayOrDefault(currentMemory, "закрытыеЛинии", "resolvedThreads");
        var knownNpcs = GetArrayOrDefault(currentMemory, "известныеNpc", "knownNpcs");
        var knownLocations = GetArrayOrDefault(currentMemory, "известныеЛокации", "knownLocations");
        var masterSecrets = GetArrayOrDefault(currentMemory, "секретыМастера", "masterSecrets");

        var summaryAppend = GetOptionalString(payload, "summaryAppend", "добавитьКРезюме");
        if (!string.IsNullOrWhiteSpace(summaryAppend))
        {
            summary = string.IsNullOrWhiteSpace(summary)
                ? summaryAppend.Trim()
                : $"{summary.TrimEnd()}{Environment.NewLine}{summaryAppend.Trim()}";
            updated.Add("summary");
        }

        if (TryGetOptionalElement(payload, out var currentScenePatch, "currentScene", "текущаяСцена"))
        {
            if (currentScenePatch.ValueKind is not JsonValueKind.Object)
            {
                throw new RpgValidationException("currentScene/текущаяСцена должен быть JSON object.");
            }

            currentScene = ShallowMergeObject(currentScene, currentScenePatch);
            updated.Add("currentScene");
        }

        importantFacts = MergeArrayField(payload, importantFacts, updated, "importantFacts", "importantFactsAdd", "важныеФактыДобавить");
        openThreads = MergeArrayField(payload, openThreads, updated, "openThreads", "openThreadsAdd", "открытыеЛинииДобавить");
        resolvedThreads = MergeArrayField(payload, resolvedThreads, updated, "resolvedThreads", "resolvedThreadsAdd", "закрытыеЛинииДобавить");
        knownNpcs = MergeArrayField(payload, knownNpcs, updated, "knownNpcs", "knownNpcsUpsert", "известныеNpcОбновить");
        knownLocations = MergeArrayField(payload, knownLocations, updated, "knownLocations", "knownLocationsUpsert", "известныеЛокацииОбновить");
        masterSecrets = MergeArrayField(payload, masterSecrets, updated, "masterSecrets", "masterSecretsAdd", "секретыМастераДобавить");

        if (updated.Count == 0)
        {
            throw new RpgValidationException("payload обновить_память не содержит поддерживаемых полей.");
        }

        return new CampaignMemoryMergedPatch(
            summary,
            currentScene,
            importantFacts,
            openThreads,
            resolvedThreads,
            knownNpcs,
            knownLocations,
            masterSecrets,
            updated);
    }

    private static JsonElement MergeArrayField(JsonElement payload, JsonElement current, List<string> updated, string fieldName, params string[] aliases)
    {
        if (!TryGetOptionalElement(payload, out var additions, aliases))
        {
            return current;
        }

        if (additions.ValueKind is not JsonValueKind.Array)
        {
            throw new RpgValidationException($"{aliases[0]} должен быть JSON array.");
        }

        updated.Add(fieldName);
        return AppendDedup(current, additions);
    }

    private static JsonElement ShallowMergeObject(JsonElement current, JsonElement patch)
    {
        var merged = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in current.EnumerateObject())
        {
            merged[property.Name] = property.Value.Clone();
        }

        foreach (var property in patch.EnumerateObject())
        {
            merged[property.Name] = property.Value.Clone();
        }

        return JsonSerializer.SerializeToElement(merged);
    }

    private static JsonElement AppendDedup(JsonElement current, JsonElement additions)
    {
        var result = new List<JsonElement>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in current.EnumerateArray().Concat(additions.EnumerateArray()))
        {
            var key = DedupKey(item);
            if (seen.Add(key))
            {
                result.Add(item.Clone());
            }
        }

        return JsonSerializer.SerializeToElement(result);
    }

    private static string DedupKey(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var propertyName in new[] { "name", "название", "title" })
            {
                if (element.TryGetProperty(propertyName, out var property)
                    && property.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(property.GetString()))
                {
                    return $"named:{property.GetString()!.Trim()}";
                }
            }
        }

        return $"raw:{element.GetRawText()}";
    }

    private static string? GetOptionalString(JsonElement source, params string[] aliases)
    {
        if (!TryGetOptionalElement(source, out var element, aliases) || element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            throw new RpgValidationException($"{aliases[0]} должен быть string.");
        }

        return element.GetString();
    }

    private static JsonElement GetObjectOrDefault(JsonElement source, params string[] aliases)
    {
        if (!TryGetOptionalElement(source, out var element, aliases) || element.ValueKind == JsonValueKind.Null)
        {
            return JsonSerializer.SerializeToElement(new Dictionary<string, object>());
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new RpgValidationException($"{aliases[0]} должен быть JSON object.");
        }

        return element.Clone();
    }

    private static JsonElement GetArrayOrDefault(JsonElement source, params string[] aliases)
    {
        if (!TryGetOptionalElement(source, out var element, aliases) || element.ValueKind == JsonValueKind.Null)
        {
            return JsonSerializer.SerializeToElement(Array.Empty<object>());
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new RpgValidationException($"{aliases[0]} должен быть JSON array.");
        }

        return element.Clone();
    }

    private static bool TryGetOptionalElement(JsonElement source, out JsonElement element, params string[] aliases)
    {
        if (source.ValueKind != JsonValueKind.Object)
        {
            element = default;
            return false;
        }

        foreach (var alias in aliases)
        {
            if (source.TryGetProperty(alias, out element))
            {
                return true;
            }
        }

        element = default;
        return false;
    }
}
