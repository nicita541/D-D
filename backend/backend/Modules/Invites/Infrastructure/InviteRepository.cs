using backend.Infrastructure.Database;
using backend.Modules.Invites.Contracts;
using backend.Shared.Kernel;
using Npgsql;

namespace backend.Modules.Invites.Infrastructure;

public sealed class InviteRepository : IInviteRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public InviteRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RpgResult<InviteCreatedResponse>> CreateInviteAsync(
        Guid accountId,
        Guid gameStateId,
        string tokenHash,
        string rawToken,
        string role,
        int maxUses,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            INSERT INTO game.invites (
                game_state_id,
                token_hash,
                role,
                max_uses,
                expires_at,
                created_by_account_id
            )
            SELECT
                gs.id,
                @tokenHash,
                @role,
                @maxUses,
                @expiresAt,
                @accountId
            FROM game.game_states gs
            WHERE gs.id = @gameStateId
              AND (
                  gs.account_id = @accountId
                  OR EXISTS (
                      SELECT 1
                      FROM game.party_members pm
                      WHERE pm.game_state_id = gs.id
                        AND pm.account_id = @accountId
                        AND pm.status = 'active'
                        AND pm.role = 'host'
                  )
              )
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("tokenHash", tokenHash);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("maxUses", maxUses);
        command.Parameters.AddWithValue("expiresAt", expiresAt);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        if (value is not Guid inviteId)
        {
            return RpgResult<InviteCreatedResponse>.NotFound("GameState не найден или вы не host.");
        }

        return RpgResult<InviteCreatedResponse>.Ok(new InviteCreatedResponse(
            inviteId,
            gameStateId,
            rawToken,
            role,
            maxUses,
            expiresAt));
    }

    public async Task<RpgResult<InvitePreviewResponse>> PreviewInviteAsync(string tokenHash, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                i.id,
                i.game_state_id,
                gs.name,
                i.role,
                i.max_uses,
                i.use_count,
                i.expires_at,
                i.status
            FROM game.invites i
            JOIN game.game_states gs ON gs.id = i.game_state_id
            WHERE i.token_hash = @tokenHash
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tokenHash", tokenHash);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return RpgResult<InvitePreviewResponse>.NotFound("Invite не найден.");
        }

        var status = reader.GetString(7);
        var expiresAt = reader.GetFieldValue<DateTimeOffset>(6);
        var maxUses = reader.GetInt32(4);
        var useCount = reader.GetInt32(5);
        if (!string.Equals(status, "active", StringComparison.OrdinalIgnoreCase) || expiresAt <= DateTimeOffset.UtcNow || useCount >= maxUses)
        {
            return RpgResult<InvitePreviewResponse>.Conflict("Invite больше не активен.");
        }

        return RpgResult<InvitePreviewResponse>.Ok(new InvitePreviewResponse(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.GetString(3),
            maxUses - useCount,
            expiresAt));
    }

    public async Task<RpgResult<InviteAcceptedResponse>> AcceptInviteAsync(
        Guid accountId,
        string tokenHash,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var invite = await GetInviteForUpdateAsync(connection, transaction, tokenHash, cancellationToken);
            if (invite is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return RpgResult<InviteAcceptedResponse>.NotFound("Invite не найден.");
            }

            if (!invite.IsActive)
            {
                await transaction.RollbackAsync(cancellationToken);
                return RpgResult<InviteAcceptedResponse>.Conflict("Invite больше не активен.");
            }

            var existingMember = await FindActiveMemberAsync(connection, transaction, invite.GameStateId, accountId, cancellationToken);
            if (existingMember is not null)
            {
                await InsertAcceptanceAsync(connection, transaction, invite.Id, accountId, existingMember.Id, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return RpgResult<InviteAcceptedResponse>.Ok(new InviteAcceptedResponse(
                    invite.GameStateId,
                    existingMember.Id,
                    existingMember.Role,
                    "Вы уже состоите в этой партии."));
            }

            if (invite.UseCount >= invite.MaxUses)
            {
                await transaction.RollbackAsync(cancellationToken);
                return RpgResult<InviteAcceptedResponse>.Conflict("Invite уже использован максимальное число раз.");
            }

            var partyId = await EnsurePartyAsync(connection, transaction, invite.GameStateId, cancellationToken);
            if (!partyId.HasValue)
            {
                await transaction.RollbackAsync(cancellationToken);
                return RpgResult<InviteAcceptedResponse>.NotFound("Игра для invite не найдена.");
            }

            var memberId = await InsertMemberAsync(connection, transaction, invite.GameStateId, partyId.Value, accountId, invite.Role, cancellationToken);
            await IncrementUseCountAsync(connection, transaction, invite.Id, cancellationToken);
            await InsertAcceptanceAsync(connection, transaction, invite.Id, accountId, memberId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return RpgResult<InviteAcceptedResponse>.Ok(new InviteAcceptedResponse(
                invite.GameStateId,
                memberId,
                invite.Role,
                "Вы присоединились к партии."));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<RpgResult<bool>> RevokeInviteAsync(
        Guid accountId,
        Guid gameStateId,
        Guid inviteId,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            UPDATE game.invites i
            SET status = 'revoked',
                revoked_at = now(),
                revoked_by_account_id = @accountId
            FROM game.game_states gs
            WHERE gs.id = i.game_state_id
              AND (
                  gs.account_id = @accountId
                  OR EXISTS (
                      SELECT 1
                      FROM game.party_members pm
                      WHERE pm.game_state_id = gs.id
                        AND pm.account_id = @accountId
                        AND pm.status = 'active'
                        AND pm.role = 'host'
                  )
              )
              AND i.game_state_id = @gameStateId
              AND i.id = @inviteId
              AND i.status = 'active';
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("inviteId", inviteId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0
            ? RpgResult<bool>.Ok(true)
            : RpgResult<bool>.NotFound("Invite не найден или уже отозван.");
    }

    private static async Task<InviteRecord?> GetInviteForUpdateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string tokenHash,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                id,
                game_state_id,
                role,
                max_uses,
                use_count,
                expires_at,
                status
            FROM game.invites
            WHERE token_hash = @tokenHash
            FOR UPDATE;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("tokenHash", tokenHash);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new InviteRecord(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            reader.GetFieldValue<DateTimeOffset>(5),
            reader.GetString(6));
    }

    private static async Task<MemberRecord?> FindActiveMemberAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, role
            FROM game.party_members
            WHERE game_state_id = @gameStateId
              AND account_id = @accountId
              AND status = 'active'
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("accountId", accountId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new MemberRecord(reader.GetGuid(0), reader.GetString(1))
            : null;
    }

    private static async Task<Guid?> EnsurePartyAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.parties (game_state_id, name)
            SELECT id, 'Партия'
            FROM game.game_states
            WHERE id = @gameStateId
            ON CONFLICT (game_state_id)
            DO UPDATE SET updated_at = game.parties.updated_at
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is Guid id ? id : null;
    }

    private static async Task<Guid> InsertMemberAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        Guid partyId,
        Guid accountId,
        string role,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.party_members (
                game_state_id,
                party_id,
                account_id,
                role,
                status,
                display_name
            )
            SELECT
                @gameStateId,
                @partyId,
                a.id,
                @role,
                'active',
                COALESCE(NULLIF(a.display_name, ''), a.username)
            FROM auth.accounts a
            WHERE a.id = @accountId
              AND a.is_active
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("partyId", partyId);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("role", role);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is Guid id
            ? id
            : throw new RpgValidationException("Аккаунт для invite не найден или отключён.");
    }

    private static async Task IncrementUseCountAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid inviteId,
        CancellationToken cancellationToken)
    {
        const string sql = "UPDATE game.invites SET use_count = use_count + 1 WHERE id = @inviteId;";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("inviteId", inviteId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertAcceptanceAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid inviteId,
        Guid accountId,
        Guid partyMemberId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.invite_acceptances (invite_id, account_id, party_member_id)
            VALUES (@inviteId, @accountId, @partyMemberId)
            ON CONFLICT (invite_id, account_id) DO NOTHING;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("inviteId", inviteId);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("partyMemberId", partyMemberId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed record InviteRecord(
        Guid Id,
        Guid GameStateId,
        string Role,
        int MaxUses,
        int UseCount,
        DateTimeOffset ExpiresAt,
        string Status)
    {
        public bool IsActive =>
            string.Equals(Status, "active", StringComparison.OrdinalIgnoreCase)
            && ExpiresAt > DateTimeOffset.UtcNow
            && UseCount < MaxUses;
    }

    private sealed record MemberRecord(Guid Id, string Role);
}
