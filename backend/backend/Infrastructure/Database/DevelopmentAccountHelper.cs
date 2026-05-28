using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace backend.Infrastructure.Database;

public static class DevelopmentAccountHelper
{
    public const string Email = "dev-local@example.com";
    public const string Username = "dev-local";

    public static async Task<Guid> EnsureDevelopmentAccountAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var accountId = Guid.NewGuid();
        var fakeHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes($"{Username}:{accountId}"))
        ).ToLowerInvariant();

        const string sql = """
            WITH inserted AS (
                INSERT INTO auth.accounts
                (
                    id,
                    email,
                    username,
                    password_hash,
                    display_name
                )
                VALUES
                (
                    @id,
                    'dev-local@example.com',
                    'dev-local',
                    @passwordHash,
                    'Development Local Account'
                )
                ON CONFLICT (email) DO NOTHING
                RETURNING id
            )
            SELECT id FROM inserted
            UNION ALL
            SELECT id
            FROM auth.accounts
            WHERE email = 'dev-local@example.com'
            LIMIT 1;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", accountId);
        command.Parameters.AddWithValue("passwordHash", fakeHash);

        return (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Could not create or load development account."));
    }
}
