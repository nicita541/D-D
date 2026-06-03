using System.Text.Json;
using backend.Infrastructure.Database;
using Npgsql;

namespace backend.Modules.Characters.Infrastructure;

public sealed class CharacterProgressionRepository : ICharacterProgressionRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public CharacterProgressionRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<JsonElement?> GetProgressionAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, null, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        var state = await CharacterProgressionSql.SelectAsync(connection, null, gameStateId, characterId, cancellationToken);
        return state is null ? null : CharacterProgressionSql.ToProgressionJson(state);
    }

    public async Task<JsonElement?> AddExperienceAsync(Guid accountId, Guid gameStateId, Guid characterId, int experience, string reason, CancellationToken cancellationToken)
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

            var state = await CharacterProgressionSql.AddExperienceAsync(connection, transaction, gameStateId, characterId, experience, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return state is null ? null : CharacterProgressionSql.ToAddExperienceJson(state, experience, reason);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<JsonElement?> LevelUpAsync(Guid accountId, Guid gameStateId, Guid characterId, int? requestedNewLevel, int? hpMaxAdd, CancellationToken cancellationToken)
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

            var result = await CharacterProgressionSql.LevelUpAsync(connection, transaction, gameStateId, characterId, requestedNewLevel, hpMaxAdd, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result is null ? null : CharacterProgressionSql.ToLevelUpJson(result);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<bool> CharacterExistsAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
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

        await using var command = transaction is null
            ? new NpgsqlCommand(sql, connection)
            : new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }
}
