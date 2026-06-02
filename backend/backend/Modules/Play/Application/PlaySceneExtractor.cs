using System.Text.Json;
using backend.Modules.Play.Application;
using backend.Modules.Play.Contracts;
using backend.Modules.Play.Infrastructure;

namespace backend.Modules.Play.Application;

public static class PlaySceneExtractor
{
    public static PlaySceneDto? Extract(JsonElement? memory)
    {
        if (!memory.HasValue || memory.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var scene = GetOptionalObject(memory.Value, "scene", "сцена", "currentScene", "������������");
        if (!scene.HasValue)
        {
            return null;
        }

        return new PlaySceneDto(
            GetOptionalString(scene.Value, "title", "название"),
            GetOptionalString(scene.Value, "summary", "кратко", "описание"),
            GetOptionalString(scene.Value, "currentObjective", "текущаяЦель", "цель"),
            GetOptionalString(scene.Value, "currentThreat", "текущаяУгроза", "угроза"),
            GetOptionalGuid(scene.Value, "locationId", "локацияId"),
            GetGuidArray(scene.Value, "activeNpcIds", "активныеNpcIds"),
            GetGuidArray(scene.Value, "activeQuestIds", "активныеКвестыIds"),
            GetOptionalDateTimeOffset(scene.Value, "updatedAt", "обновлено"));
    }

    private static JsonElement? GetOptionalObject(JsonElement source, params string[] names)
    {
        foreach (var name in names)
        {
            if (source.TryGetProperty(name, out var element))
            {
                return element.ValueKind == JsonValueKind.Object ? element : null;
            }
        }

        return null;
    }

    private static string? GetOptionalString(JsonElement source, params string[] names)
    {
        foreach (var name in names)
        {
            if (source.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String)
            {
                var value = element.GetString();
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }
        }

        return null;
    }

    private static Guid? GetOptionalGuid(JsonElement source, params string[] names)
    {
        var value = GetOptionalString(source, names);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static DateTimeOffset? GetOptionalDateTimeOffset(JsonElement source, params string[] names)
    {
        var value = GetOptionalString(source, names);
        return DateTimeOffset.TryParse(value, out var result) ? result : null;
    }

    private static IReadOnlyList<Guid> GetGuidArray(JsonElement source, params string[] names)
    {
        foreach (var name in names)
        {
            if (!source.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var result = new List<Guid>();
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String && Guid.TryParse(item.GetString(), out var id))
                {
                    result.Add(id);
                }
            }

            return result;
        }

        return Array.Empty<Guid>();
    }
}
