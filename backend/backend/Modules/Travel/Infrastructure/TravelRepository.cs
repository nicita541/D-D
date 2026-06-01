using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Modules.Play;
using backend.Infrastructure.Database;
using Npgsql;
using NpgsqlTypes;

namespace backend.Modules.Travel;

public sealed class TravelRepository : ITravelRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public TravelRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<JsonElement?> GetOptionsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            WITH owned AS (
                SELECT id, current_location_id
                FROM game.game_states
                WHERE id = @gameStateId
                  AND account_id = @accountId
            )
            SELECT jsonb_build_object(
                'gameStateId', owned.id,
                'currentLocationId', owned.current_location_id,
                'currentLocation', (
                    SELECT jsonb_build_object(
                        'id', l.id,
                        'name', l.name,
                        'description', l.description
                    )
                    FROM game.locations l
                    WHERE l.game_state_id = owned.id
                      AND l.id = owned.current_location_id
                    LIMIT 1
                ),
                'exits', COALESCE((
                    SELECT jsonb_agg(
                        jsonb_build_object(
                            'id', e.id,
                            'locationId', e.location_id,
                            'direction', e.direction,
                            'targetLocationId', e.target_location_id,
                            'description', e.description,
                            'isLocked', e.is_locked,
                            'targetLocation', jsonb_build_object(
                                'id', target.id,
                                'name', target.name,
                                'description', target.description
                            )
                        )
                        ORDER BY e.direction, e.id
                    )
                    FROM game.location_exits e
                    JOIN game.locations target ON target.id = e.target_location_id
                    WHERE e.game_state_id = owned.id
                      AND e.location_id = owned.current_location_id
                ), '[]'::jsonb),
                'locations', COALESCE((
                    SELECT jsonb_agg(
                        jsonb_build_object(
                            'id', l.id,
                            'name', l.name,
                            'description', l.description
                        )
                        ORDER BY l.created_at, l.id
                    )
                    FROM game.locations l
                    WHERE l.game_state_id = owned.id
                ), '[]'::jsonb)
            )::text
            FROM owned;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    public async Task<JsonElement?> MoveAsync(
        Guid accountId,
        Guid gameStateId,
        PlayTravelRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.ResolvedTargetLocationId.HasValue)
        {
            throw new RpgValidationException("targetLocationId обязателен.");
        }

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var gameState = await GetGameStateForUpdateAsync(connection, transaction, accountId, gameStateId, cancellationToken);
            if (gameState is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var target = await GetLocationAsync(connection, transaction, gameStateId, request.ResolvedTargetLocationId.Value, cancellationToken);
            if (target is null)
            {
                throw new RpgValidationException("Целевая локация не найдена в этой игре.");
            }

            var directMove = !request.ResolvedExitId.HasValue;
            string? direction = null;
            if (request.ResolvedExitId.HasValue)
            {
                if (!gameState.Value.CurrentLocationId.HasValue)
                {
                    throw new RpgValidationException("Нельзя использовать выход: текущая локация не задана.");
                }

                var exit = await GetExitAsync(connection, transaction, gameStateId, gameState.Value.CurrentLocationId.Value, request.ResolvedExitId.Value, cancellationToken);
                if (exit is null || exit.Value.TargetLocationId != target.Value.Id)
                {
                    throw new RpgValidationException("Выход не ведет в указанную локацию.");
                }

                if (exit.Value.IsLocked)
                {
                    throw new RpgValidationException("Выход закрыт.");
                }

                direction = exit.Value.Direction;
            }

            await UpdateCurrentLocationAsync(connection, transaction, gameStateId, target.Value.Id, cancellationToken);
            await InsertTravelLogAsync(connection, transaction, gameStateId, gameState.Value.TurnNumber, target.Value.Name, directMove, direction, request.ResolvedNote, cancellationToken);
            await UpdateSceneAsync(connection, transaction, gameStateId, target.Value, directMove, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return JsonSerializer.SerializeToElement(new
            {
                moved = true,
                gameStateId,
                targetLocationId = target.Value.Id,
                targetLocationName = target.Value.Name,
                directMove,
                direction
            });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<GameStateTravelRow?> GetGameStateForUpdateAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, current_location_id, turn_number
            FROM game.game_states
            WHERE id = @gameStateId
              AND account_id = @accountId
            FOR UPDATE;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new GameStateTravelRow(reader.GetGuid(0), reader.IsDBNull(1) ? null : reader.GetGuid(1), reader.GetInt32(2));
    }

    private static async Task<LocationRow?> GetLocationAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid locationId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, name, description
            FROM game.locations
            WHERE game_state_id = @gameStateId
              AND id = @locationId
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("locationId", locationId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new LocationRow(reader.GetGuid(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2));
    }

    private static async Task<ExitRow?> GetExitAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid currentLocationId, Guid exitId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, direction, target_location_id, is_locked
            FROM game.location_exits
            WHERE game_state_id = @gameStateId
              AND location_id = @currentLocationId
              AND id = @exitId
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("currentLocationId", currentLocationId);
        command.Parameters.AddWithValue("exitId", exitId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ExitRow(reader.GetGuid(0), reader.GetString(1), reader.GetGuid(2), reader.GetBoolean(3));
    }

    private static async Task UpdateCurrentLocationAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid locationId, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE game.game_states SET current_location_id = @locationId, updated_at = now() WHERE id = @gameStateId;";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("locationId", locationId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertTravelLogAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        int turnNumber,
        string targetName,
        bool directMove,
        string? direction,
        string note,
        CancellationToken cancellationToken)
    {
        var text = directMove
            ? $"Direct/manual travel: party moved to {targetName}."
            : $"Travel: party moved to {targetName} via {direction}.";
        if (!string.IsNullOrWhiteSpace(note))
        {
            text = $"{text} Note: {note.Trim()}";
        }

        const string sql = """
            INSERT INTO game.game_log_entries (game_state_id, turn_number, type, text, important)
            VALUES (@gameStateId, @turnNumber, 'travel', @text, false);
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("turnNumber", Math.Max(0, turnNumber));
        command.Parameters.AddWithValue("text", text);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpdateSceneAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, LocationRow target, bool directMove, CancellationToken cancellationToken)
    {
        await using (var ensure = new NpgsqlCommand(
            "INSERT INTO game.campaign_memories (game_state_id) VALUES (@gameStateId) ON CONFLICT (game_state_id) DO NOTHING;",
            connection,
            transaction))
        {
            ensure.Parameters.AddWithValue("gameStateId", gameStateId);
            await ensure.ExecuteNonQueryAsync(cancellationToken);
        }

        var scene = JsonSerializer.SerializeToElement(new
        {
            title = target.Name,
            summary = target.Description ?? (directMove ? "Direct/manual travel destination." : "Travel destination."),
            currentObjective = "Осмотреться и выбрать следующее действие.",
            currentThreat = (string?)null,
            locationId = target.Id,
            activeNpcIds = Array.Empty<Guid>(),
            activeQuestIds = Array.Empty<Guid>(),
            updatedAt = DateTimeOffset.UtcNow,
            travelMode = directMove ? "direct_manual" : "exit"
        });

        const string sql = """
            UPDATE game.campaign_memories
            SET current_scene = @scene,
                updated_at = now()
            WHERE game_state_id = @gameStateId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        var sceneParameter = command.Parameters.Add("scene", NpgsqlDbType.Jsonb);
        sceneParameter.Value = scene.GetRawText();
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private readonly record struct GameStateTravelRow(Guid Id, Guid? CurrentLocationId, int TurnNumber);
    private readonly record struct LocationRow(Guid Id, string Name, string? Description);
    private readonly record struct ExitRow(Guid Id, string Direction, Guid TargetLocationId, bool IsLocked);
}
