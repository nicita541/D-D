using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace backend.Infrastructure.Database;

internal static class RpgDbJson
{
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
    };

    public static JsonElement ParseElement(string json)
    {
        using var document = JsonDocument.Parse(json, DocumentOptions);
        return document.RootElement.Clone();
    }

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, SerializerOptions);
    }

    public static void AddJsonb<T>(this NpgsqlParameterCollection parameters, string name, T value)
    {
        var parameter = parameters.Add(name, NpgsqlDbType.Jsonb);
        parameter.Value = Serialize(value);
    }

    public static void AddNullableUuid(this NpgsqlParameterCollection parameters, string name, Guid? value)
    {
        var parameter = parameters.Add(name, NpgsqlDbType.Uuid);
        parameter.Value = value.HasValue ? value.Value : DBNull.Value;
    }

    public static object DbString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
    }
}
