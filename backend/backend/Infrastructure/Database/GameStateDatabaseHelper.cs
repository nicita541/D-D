using Npgsql;

namespace backend.Infrastructure.Database;

public static class GameStateDatabaseHelper
{
    public static async Task<Guid> CreateNewGameAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid accountId,
        string saveName,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT game.create_new_game(@accountId, @saveName);
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("saveName", saveName);

        return (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("game.create_new_game did not return a game_state id."));
    }
}
