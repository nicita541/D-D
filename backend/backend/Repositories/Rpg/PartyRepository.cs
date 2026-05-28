using System.Text.Json;
using backend.Contracts.Rpg.Parties;
using backend.Infrastructure.Database;
using Npgsql;

namespace backend.Repositories.Rpg;

public sealed class PartyRepository : IPartyRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public PartyRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<JsonElement?> GetPartyAsync(Guid gameStateId, CancellationToken cancellationToken)
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
            WHERE p.game_state_id = @gameStateId
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    public async Task<Guid> CreatePartyAsync(Guid gameStateId, CreatePartyRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            INSERT INTO game.parties
            (
                id,
                game_state_id,
                name
            )
            VALUES
            (
                gen_random_uuid(),
                @gameStateId,
                @name
            )
            ON CONFLICT (game_state_id)
            DO UPDATE SET
                name = EXCLUDED.name,
                updated_at = now()
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("name", string.IsNullOrWhiteSpace(request.Name) ? "Партия" : request.Name.Trim());

        return (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Party id was not returned."));
    }

    public async Task<Guid> AddPartyMemberAsync(Guid gameStateId, AddPartyMemberRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var partyId = await EnsurePartyAsync(connection, transaction, gameStateId, cancellationToken);

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
                VALUES
                (
                    gen_random_uuid(),
                    @gameStateId,
                    @partyId,
                    @accountId,
                    @characterId,
                    @role,
                    @status,
                    @displayName
                )
                RETURNING id;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("gameStateId", gameStateId);
            command.Parameters.AddWithValue("partyId", partyId);
            command.Parameters.AddNullableUuid("accountId", request.AccountId);
            command.Parameters.AddNullableUuid("characterId", request.CharacterId);
            command.Parameters.AddWithValue("role", string.IsNullOrWhiteSpace(request.Role) ? "player" : request.Role.Trim());
            command.Parameters.AddWithValue("status", string.IsNullOrWhiteSpace(request.Status) ? "active" : request.Status.Trim());
            command.Parameters.AddWithValue("displayName", RpgDbJson.DbString(request.DisplayName));

            var memberId = (Guid)(await command.ExecuteScalarAsync(cancellationToken)
                ?? throw new InvalidOperationException("Party member id was not returned."));

            await transaction.CommitAsync(cancellationToken);
            return memberId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> RemovePartyMemberAsync(Guid gameStateId, Guid memberId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM game.party_members WHERE game_state_id = @gameStateId AND id = @memberId;";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("memberId", memberId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static async Task<Guid> EnsurePartyAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.parties (id, game_state_id, name)
            VALUES (gen_random_uuid(), @gameStateId, 'Партия')
            ON CONFLICT (game_state_id) DO UPDATE SET updated_at = game.parties.updated_at
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Party id was not returned."));
    }
}
