using System.Text.Json;
using backend.Contracts.Rpg.Combat;
using backend.Infrastructure.Database;
using Npgsql;

namespace backend.Repositories.Rpg;

public sealed class CombatRepository : ICombatRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public CombatRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<JsonElement?> GetCombatStateAsync(Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT jsonb_build_object(
                'id', c.id,
                'gameStateId', c.game_state_id,
                'активен', c.is_active,
                'раунд', c.round_number,
                'текущийУчастникId', c.current_turn_participant_id,
                'участники', COALESCE(
                    (
                        SELECT jsonb_agg(
                            jsonb_build_object(
                                'id', p.id,
                                'типАктера', p.actor_type,
                                'actorId', p.actor_id,
                                'имя', p.name,
                                'инициатива', p.initiative,
                                'хпТекущее', p.hp_current,
                                'хпМаксимум', p.hp_max,
                                'ужеДействовал', p.has_acted,
                                'состояния', p.conditions
                            ) ORDER BY p.initiative DESC, p.created_at
                        )
                        FROM game.combat_participants p
                        WHERE p.combat_state_id = c.id
                    ),
                    '[]'::jsonb
                )
            )::text
            FROM game.combat_states c
            WHERE c.game_state_id = @gameStateId
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    public async Task<Guid> StartCombatAsync(Guid gameStateId, StartCombatRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var combatId = await EnsureCombatAsync(connection, transaction, gameStateId, isActive: true, cancellationToken);

            await using (var delete = new NpgsqlCommand(
                "DELETE FROM game.combat_participants WHERE game_state_id = @gameStateId AND combat_state_id = @combatId;",
                connection,
                transaction))
            {
                delete.Parameters.AddWithValue("gameStateId", gameStateId);
                delete.Parameters.AddWithValue("combatId", combatId);
                await delete.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var participant in request.Participants)
            {
                await InsertParticipantAsync(connection, transaction, gameStateId, combatId, participant, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return combatId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> EndCombatAsync(Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE game.combat_states
            SET is_active = false,
                updated_at = now()
            WHERE game_state_id = @gameStateId
              AND is_active = true;
        """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<Guid> AddParticipantAsync(Guid gameStateId, AddCombatParticipantRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var combatId = await EnsureCombatAsync(connection, transaction, gameStateId, isActive: true, cancellationToken);
            var participantId = await InsertParticipantAsync(connection, transaction, gameStateId, combatId, request, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return participantId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<Guid> EnsureCombatAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, bool isActive, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.combat_states
            (
                id,
                game_state_id,
                is_active,
                round_number
            )
            VALUES
            (
                gen_random_uuid(),
                @gameStateId,
                @isActive,
                1
            )
            ON CONFLICT (game_state_id)
            DO UPDATE SET
                is_active = EXCLUDED.is_active,
                updated_at = now()
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("isActive", isActive);
        return (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Combat state id was not returned."));
    }

    private static async Task<Guid> InsertParticipantAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        Guid combatId,
        AddCombatParticipantRequest request,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.combat_participants
            (
                id,
                game_state_id,
                combat_state_id,
                actor_type,
                actor_id,
                name,
                initiative,
                hp_current,
                hp_max,
                has_acted,
                conditions
            )
            VALUES
            (
                gen_random_uuid(),
                @gameStateId,
                @combatId,
                @actorType,
                @actorId,
                @name,
                @initiative,
                @hpCurrent,
                @hpMax,
                false,
                @conditions::jsonb
            )
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("combatId", combatId);
        command.Parameters.AddWithValue("actorType", string.IsNullOrWhiteSpace(request.ActorType) ? "character" : request.ActorType.Trim());
        command.Parameters.AddWithValue("actorId", request.ActorId);
        command.Parameters.AddWithValue("name", string.IsNullOrWhiteSpace(request.Name) ? "Безымянный" : request.Name.Trim());
        command.Parameters.AddWithValue("initiative", request.Initiative);
        command.Parameters.AddWithValue("hpCurrent", request.HpCurrent);
        command.Parameters.AddWithValue("hpMax", request.HpMax);
        command.Parameters.AddJsonb("conditions", request.Conditions);

        return (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Combat participant id was not returned."));
    }
}
