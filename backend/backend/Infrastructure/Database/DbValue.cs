using Npgsql;
using NpgsqlTypes;

namespace backend.Infrastructure.Database;

public static class DbValue
{
    public static Guid ParseGuidOrNew(string? value)
    {
        return Guid.TryParse(value, out var parsed)
            ? parsed
            : Guid.NewGuid();
    }

    public static Guid? ParseNullableGuid(string? value)
    {
        return Guid.TryParse(value, out var parsed)
            ? parsed
            : null;
    }

    public static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public static object ToDb(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
    }

    public static object ToDb(int? value)
    {
        return value.HasValue ? value.Value : DBNull.Value;
    }

    public static void AddUuidParameter(NpgsqlCommand command, string name, Guid? value)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Uuid);
        parameter.Value = value.HasValue ? value.Value : DBNull.Value;
    }
}
