using System.Text.Json;
using backend.Infrastructure.Database;
using backend.Services.Rpg;
using Npgsql;
using NpgsqlTypes;

namespace backend.Repositories.Rpg;

public sealed class DiceRollRepository : IDiceRollRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public DiceRollRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<JsonElement?> CreateRollAsync(
        Guid accountId,
        Guid gameStateId,
        Guid? characterId,
        string reason,
        DiceRollResult roll,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        if (!await GameStateExistsAsync(connection, accountId, gameStateId, cancellationToken))
        {
            return null;
        }

        if (characterId.HasValue && !await CharacterExistsAsync(connection, gameStateId, characterId.Value, cancellationToken))
        {
            return null;
        }

        const string sql = """
            INSERT INTO game.dice_rolls
            (
                game_state_id,
                account_id,
                character_id,
                formula,
                reason,
                dice_count,
                dice_sides,
                modifier,
                rolls,
                total
            )
            VALUES
            (
                @gameStateId,
                @accountId,
                @characterId,
                @formula,
                @reason,
                @diceCount,
                @diceSides,
                @modifier,
                @rolls,
                @total
            )
            RETURNING jsonb_build_object(
                'id', id,
                'gameStateId', game_state_id,
                'characterId', character_id,
                'formula', formula,
                'reason', reason,
                'rolls', rolls,
                'modifier', modifier,
                'total', total,
                'createdAt', created_at
            )::text;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        AddRollParameters(command, accountId, gameStateId, characterId, reason, roll);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    public async Task<IReadOnlyList<JsonElement>?> GetRollsAsync(
        Guid accountId,
        Guid gameStateId,
        int limit,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await GameStateExistsAsync(connection, accountId, gameStateId, cancellationToken))
        {
            return null;
        }

        const string sql = """
            SELECT jsonb_build_object(
                'id', id,
                'gameStateId', game_state_id,
                'characterId', character_id,
                'formula', formula,
                'reason', reason,
                'rolls', rolls,
                'modifier', modifier,
                'total', total,
                'createdAt', created_at
            )::text
            FROM game.dice_rolls
            WHERE game_state_id = @gameStateId
              AND account_id = @accountId
            ORDER BY created_at DESC, id DESC
            LIMIT @limit;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("limit", Math.Clamp(limit, 1, 100));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<JsonElement>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(RpgDbJson.ParseElement(reader.GetString(0)));
        }

        return result;
    }

    internal static void AddRollParameters(
        NpgsqlCommand command,
        Guid accountId,
        Guid gameStateId,
        Guid? characterId,
        string reason,
        DiceRollResult roll)
    {
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        var characterParameter = command.Parameters.Add("characterId", NpgsqlDbType.Uuid);
        characterParameter.Value = characterId.HasValue ? characterId.Value : DBNull.Value;
        command.Parameters.AddWithValue("formula", roll.Formula.Normalized);
        command.Parameters.AddWithValue("reason", string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim());
        command.Parameters.AddWithValue("diceCount", roll.Formula.DiceCount);
        command.Parameters.AddWithValue("diceSides", roll.Formula.DiceSides);
        command.Parameters.AddWithValue("modifier", roll.Formula.Modifier);
        var rollsParameter = command.Parameters.Add("rolls", NpgsqlDbType.Jsonb);
        rollsParameter.Value = JsonSerializer.Serialize(roll.Rolls);
        command.Parameters.AddWithValue("total", roll.Total);
    }

    private static async Task<bool> GameStateExistsAsync(NpgsqlConnection connection, Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM game.game_states WHERE id = @gameStateId AND account_id = @accountId);";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task<bool> CharacterExistsAsync(NpgsqlConnection connection, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM game.players WHERE id = @characterId AND game_state_id = @gameStateId);";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }
}
