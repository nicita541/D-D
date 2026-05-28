using backend.Infrastructure.Database;
using Npgsql;

namespace backend.Repositories.Auth;

public sealed class AuthRepository : IAuthRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public AuthRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AccountRecord?> FindAccountByIdAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT id, email, username, password_hash, display_name, role, is_active
            FROM auth.accounts
            WHERE id = @accountId
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadAccount(reader) : null;
    }

    public async Task<AccountRecord?> FindAccountByEmailOrUsernameAsync(string emailOrUsername, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT id, email, username, password_hash, display_name, role, is_active
            FROM auth.accounts
            WHERE lower(email) = lower(@emailOrUsername)
               OR lower(username) = lower(@emailOrUsername)
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("emailOrUsername", emailOrUsername.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadAccount(reader) : null;
    }

    public async Task<AccountRecord> CreateAccountAsync(
        string email,
        string username,
        string passwordHash,
        string? displayName,
        string role,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            INSERT INTO auth.accounts
            (
                email,
                username,
                password_hash,
                display_name,
                role
            )
            VALUES
            (
                @email,
                @username,
                @passwordHash,
                @displayName,
                @role
            )
            RETURNING id, email, username, password_hash, display_name, role, is_active;
        """;

        try
        {
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("email", email.Trim());
            command.Parameters.AddWithValue("username", username.Trim());
            command.Parameters.AddWithValue("passwordHash", passwordHash);
            command.Parameters.AddWithValue("displayName", RpgDbJson.DbString(displayName));
            command.Parameters.AddWithValue("role", string.IsNullOrWhiteSpace(role) ? "user" : role.Trim());

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Account was not returned after insert.");
            }

            return ReadAccount(reader);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateAccountException();
        }
    }

    public async Task StoreRefreshTokenAsync(
        Guid accountId,
        string tokenHash,
        DateTimeOffset expiresAt,
        string? createdByIp,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            INSERT INTO auth.refresh_tokens
            (
                account_id,
                token_hash,
                expires_at,
                created_by_ip
            )
            VALUES
            (
                @accountId,
                @tokenHash,
                @expiresAt,
                @createdByIp
            );
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("tokenHash", tokenHash);
        command.Parameters.AddWithValue("expiresAt", expiresAt);
        command.Parameters.AddWithValue("createdByIp", RpgDbJson.DbString(createdByIp));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<RefreshTokenRecord?> FindRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                rt.id,
                rt.account_id,
                rt.token_hash,
                rt.expires_at,
                rt.revoked_at,
                a.id,
                a.email,
                a.username,
                a.password_hash,
                a.display_name,
                a.role,
                a.is_active
            FROM auth.refresh_tokens rt
            JOIN auth.accounts a ON a.id = rt.account_id
            WHERE rt.token_hash = @tokenHash
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tokenHash", tokenHash);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var account = new AccountRecord(
            reader.GetGuid(5),
            reader.GetString(6),
            reader.GetString(7),
            reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.GetString(10),
            reader.GetBoolean(11));

        return new RefreshTokenRecord(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.GetFieldValue<DateTimeOffset>(3),
            reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4),
            account);
    }

    public async Task<bool> RevokeRefreshTokenAsync(
        string tokenHash,
        string? revokedByIp,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            UPDATE auth.refresh_tokens
            SET revoked_at = COALESCE(revoked_at, now()),
                revoked_by_ip = COALESCE(@revokedByIp, revoked_by_ip)
            WHERE token_hash = @tokenHash
              AND revoked_at IS NULL;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tokenHash", tokenHash);
        command.Parameters.AddWithValue("revokedByIp", RpgDbJson.DbString(revokedByIp));
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static AccountRecord ReadAccount(NpgsqlDataReader reader)
    {
        return new AccountRecord(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.GetString(5),
            reader.GetBoolean(6));
    }
}
