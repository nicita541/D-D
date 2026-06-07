using System.Text.Json;
using System.Text.Json.Nodes;
using backend.Modules.Play.Contracts;

namespace backend.Shared.Results;

public static class SecretRedactor
{
    private static readonly HashSet<string> SecretPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "секретыМастера",
        "masterSecrets",
        "masterSecretsAdd",
        "скрытыеФакты",
        "hiddenFacts",
        "aiContext",
        "debug",
        "diagnostics",
        "prompt"
    };

    public static PlayStateResponse Redact(PlayStateResponse response)
    {
        return response with
        {
            GameState = Redact(response.GameState),
            Memory = Redact(response.Memory),
            PendingChanges = Redact(response.PendingChanges),
            MechanicRequests = Redact(response.MechanicRequests)
        };
    }

    public static IReadOnlyList<JsonElement> Redact(IReadOnlyList<JsonElement> values)
        => values.Select(value => Redact(value)).ToArray();

    public static JsonElement? Redact(JsonElement? value)
        => value.HasValue ? Redact(value.Value) : null;

    public static JsonElement Redact(JsonElement value)
    {
        var node = JsonNode.Parse(value.GetRawText());
        if (node is null)
        {
            return value.Clone();
        }

        RedactNode(node);
        return JsonSerializer.SerializeToElement(node);
    }

    private static void RedactNode(JsonNode node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var propertyName in jsonObject.Select(property => property.Key).ToArray())
            {
                if (SecretPropertyNames.Contains(propertyName))
                {
                    jsonObject.Remove(propertyName);
                    continue;
                }

                if (jsonObject[propertyName] is { } child)
                {
                    RedactNode(child);
                }
            }

            return;
        }

        if (node is JsonArray jsonArray)
        {
            foreach (var child in jsonArray)
            {
                if (child is not null)
                {
                    RedactNode(child);
                }
            }
        }
    }
}
