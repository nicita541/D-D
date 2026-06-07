using System.Text.Json;
using backend.Modules.Party.Contracts;
using backend.Infrastructure.Database;
using Npgsql;

namespace backend.Modules.Party.Infrastructure;

public sealed class PartyRepository : IPartyRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public PartyRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<JsonElement?> GetPartyAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT jsonb_build_object(
                'id', p.id,
                'gameStateId', p.game_state_id,
                'название', p.name,
                'участники', COALESCE(
                    (
                        SELECT jsonb_agg(
                            jsonb_build_object(
                                'id', m.id,
                                'accountId', m.account_id,
                                'characterId', m.character_id,
                                'роль', m.role,
                                'статус', m.status,
                                'отображаемоеИмя', m.display_name
                            ) ORDER BY m.created_at
                        )
                        FROM game.party_members m
                        WHERE m.party_id = p.id
                    ),
                    '[]'::jsonb
                )
            )::text
            FROM game.parties p
            JOIN game.game_states gs ON gs.id = p.game_state_id
            WHERE p.game_state_id = @gameStateId
              AND gs.account_id = @accountId
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    public async Task<Guid?> CreatePartyAsync(Guid accountId, Guid gameStateId, CreatePartyRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            INSERT INTO game.parties
            (
                id,
                game_state_id,
                name
            )
            SELECT
                gen_random_uuid(),
                @gameStateId,
                @name
            WHERE EXISTS (
                SELECT 1
                FROM game.game_states
                WHERE id = @gameStateId
                  AND account_id = @accountId
            )
            ON CONFLICT (game_state_id)
            DO UPDATE SET
                name = EXCLUDED.name,
                updated_at = now()
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("name", string.IsNullOrWhiteSpace(request.Name) ? "Партия" : request.Name.Trim());

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : (Guid)value;
    }

    public async Task<Guid?> AddPartyMemberAsync(Guid accountId, Guid gameStateId, AddPartyMemberRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var partyId = await EnsurePartyAsync(connection, transaction, accountId, gameStateId, cancellationToken);
            if (!partyId.HasValue)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            const string sql = """
                INSERT INTO game.party_members
                (
                    id,
                    game_state_id,
                    party_id,
                    account_id,
                    character_id,
                    role,
                    status,
                    display_name
                )
                SELECT
                    gen_random_uuid(),
                    @gameStateId,
                    @partyId,
                    @memberAccountId,
                    @characterId,
                    @role,
                    @status,
                    @displayName
                WHERE @characterId IS NULL
                   OR EXISTS (
                        SELECT 1
                        FROM game.players
                        WHERE id = @characterId
                          AND game_state_id = @gameStateId
                          AND account_id = @accountId
                   )
                RETURNING id;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("accountId", accountId);
            command.Parameters.AddWithValue("gameStateId", gameStateId);
            command.Parameters.AddWithValue("partyId", partyId.Value);
            command.Parameters.AddWithValue("memberAccountId", request.AccountId ?? accountId);
            command.Parameters.AddNullableUuid("characterId", request.CharacterId);
            command.Parameters.AddWithValue("role", string.IsNullOrWhiteSpace(request.Role) ? "player" : request.Role.Trim());
            command.Parameters.AddWithValue("status", string.IsNullOrWhiteSpace(request.Status) ? "active" : request.Status.Trim());
            command.Parameters.AddWithValue("displayName", RpgDbJson.DbString(request.DisplayName));

            var value = await command.ExecuteScalarAsync(cancellationToken);
            if (value is null or DBNull)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await transaction.CommitAsync(cancellationToken);
            return (Guid)value;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> UpdatePartyMemberAsync(Guid accountId, Guid gameStateId, Guid memberId, UpdatePartyMemberRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            UPDATE game.party_members m
            SET account_id = COALESCE(@memberAccountId, m.account_id),
                character_id = COALESCE(@characterId, m.character_id),
                role = COALESCE(@role, m.role),
                status = COALESCE(@status, m.status),
                display_name = COALESCE(@displayName, m.display_name),
                updated_at = now()
            FROM game.game_states gs
            WHERE gs.id = m.game_state_id
              AND gs.account_id = @accountId
              AND m.game_state_id = @gameStateId
              AND m.id = @memberId
              AND (
                    @characterId IS NULL
                    OR EXISTS (
                        SELECT 1
                        FROM game.players p
                        WHERE p.id = @characterId
                          AND p.game_state_id = @gameStateId
                    )
              );
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("memberId", memberId);
        command.Parameters.AddNullableUuid("memberAccountId", request.AccountId);
        command.Parameters.AddNullableUuid("characterId", request.CharacterId);
        command.Parameters.AddWithValue("role", RpgDbJson.DbString(request.ResolvedRole));
        command.Parameters.AddWithValue("status", RpgDbJson.DbString(request.ResolvedStatus));
        command.Parameters.AddWithValue("displayName", RpgDbJson.DbString(request.ResolvedDisplayName));
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> AssignPartyMemberCharacterAsync(Guid accountId, Guid gameStateId, Guid memberId, Guid? characterId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            UPDATE game.party_members m
            SET character_id = @characterId,
                updated_at = now()
            FROM game.game_states gs
            WHERE gs.id = m.game_state_id
              AND gs.account_id = @accountId
              AND m.game_state_id = @gameStateId
              AND m.id = @memberId
              AND (
                    @characterId IS NULL
                    OR EXISTS (
                        SELECT 1
                        FROM game.players p
                        WHERE p.id = @characterId
                          AND p.game_state_id = @gameStateId
                    )
              );
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("memberId", memberId);
        command.Parameters.AddNullableUuid("characterId", characterId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> AssignCharacterToAccountAsync(Guid ownerAccountId, Guid gameStateId, Guid memberAccountId, Guid characterId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            UPDATE game.party_members m
            SET character_id = @characterId,
                updated_at = now()
            FROM game.game_states gs
            WHERE gs.id = m.game_state_id
              AND gs.account_id = @ownerAccountId
              AND m.game_state_id = @gameStateId
              AND m.account_id = @memberAccountId
              AND m.status = 'active'
              AND EXISTS (
                    SELECT 1
                    FROM game.players p
                    WHERE p.id = @characterId
                      AND p.game_state_id = @gameStateId
              );
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("ownerAccountId", ownerAccountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("memberAccountId", memberAccountId);
        command.Parameters.AddWithValue("characterId", characterId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> RemovePartyMemberAsync(Guid accountId, Guid gameStateId, Guid memberId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            DELETE FROM game.party_members m
            USING game.game_states gs
            WHERE gs.id = m.game_state_id
              AND gs.account_id = @accountId
              AND m.game_state_id = @gameStateId
              AND m.id = @memberId;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("memberId", memberId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> LeavePartyAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            UPDATE game.party_members m
            SET status = 'left',
                updated_at = now()
            FROM game.game_states gs
            WHERE gs.id = m.game_state_id
              AND gs.account_id <> @accountId
              AND m.account_id = @accountId
              AND m.game_state_id = @gameStateId
              AND m.status = 'active';
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static async Task<Guid?> EnsurePartyAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid accountId,
        Guid gameStateId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.parties (id, game_state_id, name)
            SELECT gen_random_uuid(), @gameStateId, 'Партия'
            WHERE EXISTS (
                SELECT 1
                FROM game.game_states
                WHERE id = @gameStateId
                  AND account_id = @accountId
            )
            ON CONFLICT (game_state_id) DO UPDATE SET updated_at = game.parties.updated_at
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : (Guid)value;
    }
}
