using System.Text.Json;
using backend.Infrastructure.Database;
using backend.Modules.Ai.Application;
using backend.Modules.Campaigns.Application;
using backend.Modules.Changes.Application;
using backend.Modules.Characters.Application;
using backend.Modules.Combat.Application;
using backend.Modules.GameStates.Application;
using backend.Modules.Mechanics.Application;
using backend.Modules.Memory.Application;
using backend.Modules.Party.Application;
using backend.Modules.Play.Application;
using backend.Modules.Story.Application;
using backend.Modules.Travel.Application;
using backend.Modules.Turns.Application;
using backend.Modules.World.Application;
using Npgsql;

namespace backend.Modules.Mechanics.Infrastructure;

public sealed class AbilityCheckRepository : IAbilityCheckRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public AbilityCheckRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int?> GetAbilityScoreAsync(
        Guid accountId,
        Guid gameStateId,
        Guid characterId,
        string normalizedAbility,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var column = AbilityRules.ToColumnName(normalizedAbility);
        var sql = $"""
            SELECT pa.{column}
            FROM game.player_attributes pa
            JOIN game.players p ON p.id = pa.player_id AND p.game_state_id = pa.game_state_id
            JOIN game.game_states gs ON gs.id = p.game_state_id
            WHERE gs.account_id = @accountId
              AND p.game_state_id = @gameStateId
              AND p.id = @characterId
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToInt32(value);
    }

    public async Task<JsonElement?> CreateAbilityCheckAsync(
        Guid accountId,
        Guid gameStateId,
        Guid characterId,
        string ability,
        int abilityScore,
        int modifier,
        int difficultyClass,
        bool success,
        string reason,
        DiceRollResult roll,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!await CharacterExistsAsync(connection, transaction, accountId, gameStateId, characterId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            const string insertRollSql = """
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
                RETURNING id;
            """;

            Guid rollId;
            await using (var rollCommand = new NpgsqlCommand(insertRollSql, connection, transaction))
            {
                DiceRollRepository.AddRollParameters(rollCommand, accountId, gameStateId, characterId, reason, roll);
                rollId = (Guid)(await rollCommand.ExecuteScalarAsync(cancellationToken)
                    ?? throw new InvalidOperationException("Dice roll id was not returned."));
            }

            const string insertCheckSql = """
                INSERT INTO game.skill_checks
                (
                    game_state_id,
                    account_id,
                    character_id,
                    roll_id,
                    ability,
                    difficulty_class,
                    total,
                    success,
                    reason
                )
                VALUES
                (
                    @gameStateId,
                    @accountId,
                    @characterId,
                    @rollId,
                    @ability,
                    @difficultyClass,
                    @total,
                    @success,
                    @reason
                )
                RETURNING id;
            """;

            Guid checkId;
            await using (var checkCommand = new NpgsqlCommand(insertCheckSql, connection, transaction))
            {
                checkCommand.Parameters.AddWithValue("gameStateId", gameStateId);
                checkCommand.Parameters.AddWithValue("accountId", accountId);
                checkCommand.Parameters.AddWithValue("characterId", characterId);
                checkCommand.Parameters.AddWithValue("rollId", rollId);
                checkCommand.Parameters.AddWithValue("ability", ability);
                checkCommand.Parameters.AddWithValue("difficultyClass", difficultyClass);
                checkCommand.Parameters.AddWithValue("total", roll.Total);
                checkCommand.Parameters.AddWithValue("success", success);
                checkCommand.Parameters.AddWithValue("reason", string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim());
                checkId = (Guid)(await checkCommand.ExecuteScalarAsync(cancellationToken)
                    ?? throw new InvalidOperationException("Skill check id was not returned."));
            }

            await transaction.CommitAsync(cancellationToken);

            return JsonSerializer.SerializeToElement(new
            {
                id = checkId,
                roll = new
                {
                    id = rollId,
                    gameStateId,
                    characterId,
                    formula = roll.Formula.Normalized,
                    reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim(),
                    rolls = roll.Rolls,
                    modifier,
                    total = roll.Total
                },
                characterId,
                ability,
                abilityScore,
                modifier,
                difficultyClass,
                total = roll.Total,
                success,
                reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim()
            });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<JsonElement>?> GetChecksAsync(
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
                'id', sc.id,
                'roll', jsonb_build_object(
                    'id', dr.id,
                    'gameStateId', dr.game_state_id,
                    'characterId', dr.character_id,
                    'formula', dr.formula,
                    'reason', dr.reason,
                    'rolls', dr.rolls,
                    'modifier', dr.modifier,
                    'total', dr.total,
                    'createdAt', dr.created_at
                ),
                'characterId', sc.character_id,
                'ability', sc.ability,
                'difficultyClass', sc.difficulty_class,
                'total', sc.total,
                'success', sc.success,
                'reason', sc.reason,
                'createdAt', sc.created_at
            )::text
            FROM game.skill_checks sc
            JOIN game.dice_rolls dr ON dr.id = sc.roll_id
            WHERE sc.game_state_id = @gameStateId
              AND sc.account_id = @accountId
            ORDER BY sc.created_at DESC, sc.id DESC
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

    private static async Task<bool> CharacterExistsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid accountId,
        Guid gameStateId,
        Guid characterId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM game.players p
                JOIN game.game_states gs ON gs.id = p.game_state_id
                WHERE gs.account_id = @accountId
                  AND p.game_state_id = @gameStateId
                  AND p.id = @characterId
            );
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task<bool> GameStateExistsAsync(NpgsqlConnection connection, Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM game.game_states WHERE id = @gameStateId AND account_id = @accountId);";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }
}
