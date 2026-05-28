using System.Text.Json;

namespace backend.Infrastructure.Database;

public static class JsonElementParser
{
    private static readonly JsonDocumentOptions Options = new()
    {
        AllowTrailingCommas = true
    };

    public static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json, Options);
        return document.RootElement.Clone();
    }
}
