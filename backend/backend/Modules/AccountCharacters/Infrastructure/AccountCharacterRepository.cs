using System.Text.Json;
using backend.Infrastructure.Database;
using backend.Modules.AccountCharacters.Contracts;
using Npgsql;
using NpgsqlTypes;

namespace backend.Modules.AccountCharacters.Infrastructure;

public sealed class AccountCharacterRepository : IAccountCharacterRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public AccountCharacterRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<JsonElement>> GetCharactersAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"SELECT {AccountCharacterJsonExpression} FROM game.account_characters WHERE account_id = @accountId ORDER BY updated_at DESC, name;";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<JsonElement>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(RpgDbJson.ParseElement(reader.GetString(0)));
        }

        return result;
    }

    public async Task<JsonElement?> GetCharacterAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"SELECT {AccountCharacterJsonExpression} FROM game.account_characters WHERE account_id = @accountId AND id = @characterId LIMIT 1;";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("characterId", characterId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    public async Task<JsonElement?> CreateCharacterAsync(Guid accountId, AccountCharacterDraft draft, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO game.account_characters
            (
                account_id,
                name,
                species,
                class_name,
                background,
                description,
                alignment,
                attributes,
                resources,
                progression,
                wealth,
                combat,
                inventory,
                equipment,
                attacks,
                metadata
            )
            VALUES
            (
                @accountId,
                @name,
                @species,
                @className,
                @background,
                @description,
                @alignment,
                @attributes::jsonb,
                @resources::jsonb,
                @progression::jsonb,
                @wealth::jsonb,
                @combat::jsonb,
                @inventory::jsonb,
                @equipment::jsonb,
                @attacks::jsonb,
                @metadata::jsonb
            )
            RETURNING
                jsonb_build_object(
                    'id', id,
                    'accountId', account_id,
                    'name', name,
                    'species', species,
                    'className', class_name,
                    'background', background,
                    'description', description,
                    'alignment', alignment,
                    'attributes', attributes,
                    'resources', resources,
                    'progression', progression,
                    'wealth', wealth,
                    'combat', combat,
                    'inventory', inventory,
                    'equipment', equipment,
                    'attacks', attacks,
                    'metadata', metadata,
                    'version', version,
                    'createdAt', created_at,
                    'updatedAt', updated_at
                )::text;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("name", draft.Name);
        command.Parameters.AddWithValue("species", RpgDbJson.DbString(draft.Species));
        command.Parameters.AddWithValue("className", RpgDbJson.DbString(draft.ClassName));
        command.Parameters.AddWithValue("background", RpgDbJson.DbString(draft.Background));
        command.Parameters.AddWithValue("description", RpgDbJson.DbString(draft.Description));
        command.Parameters.AddWithValue("alignment", RpgDbJson.DbString(draft.Alignment));
        AddJsonb(command, "attributes", draft.Attributes);
        AddJsonb(command, "resources", draft.Resources);
        AddJsonb(command, "progression", draft.Progression);
        AddJsonb(command, "wealth", draft.Wealth);
        AddJsonb(command, "combat", draft.Combat);
        AddJsonb(command, "inventory", draft.Inventory);
        AddJsonb(command, "equipment", draft.Equipment);
        AddJsonb(command, "attacks", draft.Attacks);
        AddJsonb(command, "metadata", draft.Metadata);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    public async Task<JsonElement?> UpdateCharacterAsync(Guid accountId, Guid characterId, UpdateAccountCharacterRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE game.account_characters
            SET name = COALESCE(NULLIF(@name, ''), name),
                species = COALESCE(@species, species),
                class_name = COALESCE(@className, class_name),
                background = COALESCE(@background, background),
                description = COALESCE(@description, description),
                alignment = COALESCE(@alignment, alignment),
                attributes = COALESCE(@attributes::jsonb, attributes),
                resources = COALESCE(@resources::jsonb, resources),
                progression = COALESCE(@progression::jsonb, progression),
                wealth = COALESCE(@wealth::jsonb, wealth),
                combat = COALESCE(@combat::jsonb, combat),
                inventory = COALESCE(@inventory::jsonb, inventory),
                equipment = COALESCE(@equipment::jsonb, equipment),
                attacks = COALESCE(@attacks::jsonb, attacks),
                metadata = COALESCE(@metadata::jsonb, metadata),
                version = version + 1,
                updated_at = now()
            WHERE account_id = @accountId
              AND id = @characterId
            RETURNING
                jsonb_build_object(
                    'id', id,
                    'accountId', account_id,
                    'name', name,
                    'species', species,
                    'className', class_name,
                    'background', background,
                    'description', description,
                    'alignment', alignment,
                    'attributes', attributes,
                    'resources', resources,
                    'progression', progression,
                    'wealth', wealth,
                    'combat', combat,
                    'inventory', inventory,
                    'equipment', equipment,
                    'attacks', attacks,
                    'metadata', metadata,
                    'version', version,
                    'createdAt', created_at,
                    'updatedAt', updated_at
                )::text;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("name", RpgDbJson.DbString(request.Name));
        command.Parameters.AddWithValue("species", RpgDbJson.DbString(request.Species));
        command.Parameters.AddWithValue("className", RpgDbJson.DbString(request.ClassName));
        command.Parameters.AddWithValue("background", RpgDbJson.DbString(request.Background));
        command.Parameters.AddWithValue("description", RpgDbJson.DbString(request.Description));
        command.Parameters.AddWithValue("alignment", RpgDbJson.DbString(request.Alignment));
        AddNullableJsonb(command, "attributes", request.Attributes);
        AddNullableJsonb(command, "resources", request.Resources);
        AddNullableJsonb(command, "progression", request.Progression);
        AddNullableJsonb(command, "wealth", request.Wealth);
        AddNullableJsonb(command, "combat", request.Combat);
        AddNullableJsonb(command, "inventory", request.Inventory);
        AddNullableJsonb(command, "equipment", request.Equipment);
        AddNullableJsonb(command, "attacks", request.Attacks);
        AddNullableJsonb(command, "metadata", request.Metadata);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    public async Task<bool> DeleteCharacterAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM game.account_characters WHERE account_id = @accountId AND id = @characterId;";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("characterId", characterId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private const string AccountCharacterJsonExpression = """
        jsonb_build_object(
            'id', id,
            'accountId', account_id,
            'name', name,
            'species', species,
            'className', class_name,
            'background', background,
            'description', description,
            'alignment', alignment,
            'attributes', attributes,
            'resources', resources,
            'progression', progression,
            'wealth', wealth,
            'combat', combat,
            'inventory', inventory,
            'equipment', equipment,
            'attacks', attacks,
            'metadata', metadata,
            'version', version,
            'createdAt', created_at,
            'updatedAt', updated_at
        )::text
        """;

    private static void AddJsonb(NpgsqlCommand command, string name, JsonElement value)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Jsonb);
        parameter.Value = value.GetRawText();
    }

    private static void AddNullableJsonb(NpgsqlCommand command, string name, JsonElement? value)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Jsonb);
        parameter.Value = value.HasValue ? value.Value.GetRawText() : DBNull.Value;
    }
}
