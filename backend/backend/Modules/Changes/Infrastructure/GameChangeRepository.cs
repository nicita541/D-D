using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Infrastructure.Database;
using backend.Modules.Changes;
using backend.Services.Rpg;
using Npgsql;
using NpgsqlTypes;

namespace backend.Modules.Changes;

public sealed class GameChangeRepository : IGameChangeRepository
{
    private static readonly HashSet<string> SupportedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "pending",
        "applied",
        "rejected"
    };

    private readonly IPostgresConnectionFactory _connectionFactory;
    private readonly GameChangeDispatcher _dispatcher;

    public GameChangeRepository(IPostgresConnectionFactory connectionFactory, GameChangeDispatcher dispatcher)
    {
        _connectionFactory = connectionFactory;
        _dispatcher = dispatcher;
    }

    public async Task<IReadOnlyList<JsonElement>?> GetChangesAsync(Guid accountId, Guid gameStateId, string? status, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(status) && !SupportedStatuses.Contains(status.Trim()))
        {
            throw new RpgValidationException("Unsupported change status.");
        }

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await GameStateExistsAsync(connection, accountId, gameStateId, cancellationToken))
        {
            return null;
        }

        const string sql = """
            SELECT jsonb_build_object(
                'id', c.id,
                'gameStateId', c.game_state_id,
                'turnId', c.game_turn_id,
                'operation', c.operation,
                'payload', c.payload,
                'status', c.status,
                'rejectReason', c.reject_reason,
                'errorMessage', c.error_message,
                'applyResult', c.apply_result,
                'createdAt', c.created_at,
                'processedAt', c.processed_at,
                'processedByAccountId', c.processed_by_account_id
            )::text
            FROM game.game_changes c
            WHERE c.game_state_id = @gameStateId
              AND (@status IS NULL OR c.status = @status)
            ORDER BY c.created_at DESC, c.id DESC;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);

        var statusParameter = command.Parameters.Add("status", NpgsqlDbType.Text);
        statusParameter.Value = string.IsNullOrWhiteSpace(status) ? DBNull.Value : status.Trim();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<JsonElement>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(RpgDbJson.ParseElement(reader.GetString(0)));
        }

        return result;
    }

    public async Task<JsonElement?> GetChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await GetChangeAsync(connection, accountId, gameStateId, changeId, cancellationToken);
    }

    public async Task<JsonElement?> ApplyChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var change = await GetPendingChangeForUpdateAsync(connection, transaction, accountId, gameStateId, changeId, cancellationToken);
            if (change is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var context = new GameChangeContext(
                connection,
                transaction,
                accountId,
                gameStateId,
                changeId,
                change.Value.GameTurnId,
                (canonicalOperation, payload, token) => ApplyCanonicalOperationAsync(
                    connection,
                    transaction,
                    accountId,
                    gameStateId,
                    changeId,
                    change.Value.GameTurnId,
                    canonicalOperation,
                    payload,
                    token));
            var result = await _dispatcher.DispatchAsync(context, change.Value.Operation, change.Value.Payload, cancellationToken);
            await MarkAppliedAsync(connection, transaction, accountId, gameStateId, changeId, result, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return await GetChangeAsync(accountId, gameStateId, changeId, cancellationToken);
    }

    public async Task<JsonElement?> RejectChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, string reason, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string sql = """
                UPDATE game.game_changes c
                SET status = 'rejected',
                    reject_reason = @reason,
                    processed_at = now(),
                    processed_by_account_id = @accountId,
                    apply_result = jsonb_build_object('rejected', true, 'reason', @reason),
                    error_message = NULL
                FROM game.game_states gs
                WHERE gs.id = c.game_state_id
                  AND gs.account_id = @accountId
                  AND c.game_state_id = @gameStateId
                  AND c.id = @changeId
                  AND c.status = 'pending';
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("accountId", accountId);
            command.Parameters.AddWithValue("gameStateId", gameStateId);
            command.Parameters.AddWithValue("changeId", changeId);
            command.Parameters.AddWithValue("reason", string.IsNullOrWhiteSpace(reason) ? "Rejected by user." : reason.Trim());
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return await GetChangeAsync(accountId, gameStateId, changeId, cancellationToken);
    }

    private static async Task<JsonElement> ApplyCanonicalOperationAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid accountId,
        Guid gameStateId,
        Guid changeId,
        Guid? gameTurnId,
        string operation,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        return operation switch
        {
            "add_item" => await AddItemAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "change_hp" => await ChangeHpAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "change_resource" => await ChangeResourceAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "add_condition" => await AddConditionAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "delete_condition" => await DeleteConditionAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "update_quest" => await UpdateQuestAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "add_journal_entry" => await AddLogEntryAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "move_item" => await MoveItemAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "request_roll" => await RequestRollAsync(connection, transaction, accountId, gameStateId, changeId, gameTurnId, payload, cancellationToken),
            "update_memory" => await UpdateMemoryAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "update_scene" => await UpdateSceneAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "create_quest" => await CreateQuestAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "create_quest_step" => await CreateQuestStepAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "complete_quest_step" => await CompleteQuestStepAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "create_location" => await CreateLocationAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "update_location" => await UpdateLocationAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "create_npc" => await CreateNpcAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "update_npc" => await UpdateNpcAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "create_world_object" => await CreateWorldObjectAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "update_world_object" => await UpdateWorldObjectAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "move_party_to_location" => await MovePartyToLocationAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "set_current_location" => await MovePartyToLocationAsync(connection, transaction, gameStateId, payload, cancellationToken),
            "open_location_exit" => await SetLocationExitLockedAsync(connection, transaction, gameStateId, payload, isLocked: false, cancellationToken),
            "unlock_location_exit" => await SetLocationExitLockedAsync(connection, transaction, gameStateId, payload, isLocked: false, cancellationToken),
            "close_location_exit" => await SetLocationExitLockedAsync(connection, transaction, gameStateId, payload, isLocked: true, cancellationToken),
            "lock_location_exit" => await SetLocationExitLockedAsync(connection, transaction, gameStateId, payload, isLocked: true, cancellationToken),
            _ => throw new RpgValidationException($"Unsupported change operation: {operation}.")
        };
    }

    private static async Task<JsonElement> RequestRollAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid accountId,
        Guid gameStateId,
        Guid changeId,
        Guid? gameTurnId,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        var requestType = GetOptionalString(payload, "���", "type") ?? "ability_check";
        if (!string.Equals(requestType, "ability_check", StringComparison.OrdinalIgnoreCase))
        {
            throw new RpgValidationException("���������_������ ������ ������������ ������ ��� ability_check.");
        }

        var ability = AbilityRules.NormalizeAbility(GetRequiredString(payload, "��������������", "ability"));
        var difficultyClass = GetOptionalInt(payload, "���������", "difficultyClass")
            ?? throw new RpgValidationException("��������� ����������� ��� ������� ������.");
        if (difficultyClass < 1)
        {
            throw new RpgValidationException("��������� ������ ���� ������ 0.");
        }

        var characterId = GetOptionalGuid(payload, "��������Id", "characterId", "character_id");
        if (characterId.HasValue)
        {
            await ValidateCharacterAsync(connection, transaction, gameStateId, characterId.Value, cancellationToken);
        }

        var reason = GetOptionalString(payload, "�������", "reason") ?? string.Empty;
        var normalizedPayload = JsonSerializer.SerializeToElement(new Dictionary<string, object?>
        {
            ["���"] = "ability_check",
            ["type"] = "ability_check",
            ["��������Id"] = characterId,
            ["characterId"] = characterId,
            ["��������������"] = ability,
            ["ability"] = ability,
            ["���������"] = difficultyClass,
            ["difficultyClass"] = difficultyClass,
            ["�������"] = reason,
            ["reason"] = reason
        });

        const string sql = """
            INSERT INTO game.mechanic_requests
            (
                game_state_id,
                account_id,
                game_turn_id,
                game_change_id,
                request_type,
                payload,
                status
            )
            VALUES
            (
                @gameStateId,
                @accountId,
                @gameTurnId,
                @changeId,
                'ability_check',
                @payload,
                'pending'
            )
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameTurnId", gameTurnId.HasValue ? gameTurnId.Value : DBNull.Value);
        command.Parameters.AddWithValue("changeId", changeId);
        AddJsonb(command, "payload", normalizedPayload.GetRawText());
        var requestId = (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Mechanic request id was not returned."));

        return JsonSerializer.SerializeToElement(new
        {
            operation = "���������_������",
            mechanicRequestId = requestId,
            requestType = "ability_check",
            status = "pending"
        });
    }

    private static async Task<JsonElement> UpdateMemoryAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        await using (var ensure = new NpgsqlCommand(
            "INSERT INTO game.campaign_memories (game_state_id) VALUES (@gameStateId) ON CONFLICT (game_state_id) DO NOTHING;",
            connection,
            transaction))
        {
            ensure.Parameters.AddWithValue("gameStateId", gameStateId);
            await ensure.ExecuteNonQueryAsync(cancellationToken);
        }

        var current = await SelectMemoryForUpdateAsync(connection, transaction, gameStateId, cancellationToken);
        var merged = CampaignMemoryMergeHelper.Merge(current, NormalizeMemoryPatch(payload));

        const string sql = """
            UPDATE game.campaign_memories
            SET summary = @summary,
                current_scene = @currentScene,
                important_facts = @importantFacts,
                open_threads = @openThreads,
                resolved_threads = @resolvedThreads,
                known_npcs = @knownNpcs,
                known_locations = @knownLocations,
                master_secrets = @masterSecrets,
                updated_at = now()
            WHERE game_state_id = @gameStateId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("summary", merged.Summary);
        AddJsonb(command, "currentScene", merged.CurrentScene.GetRawText());
        AddJsonb(command, "importantFacts", merged.ImportantFacts.GetRawText());
        AddJsonb(command, "openThreads", merged.OpenThreads.GetRawText());
        AddJsonb(command, "resolvedThreads", merged.ResolvedThreads.GetRawText());
        AddJsonb(command, "knownNpcs", merged.KnownNpcs.GetRawText());
        AddJsonb(command, "knownLocations", merged.KnownLocations.GetRawText());
        AddJsonb(command, "masterSecrets", merged.MasterSecrets.GetRawText());
        await command.ExecuteNonQueryAsync(cancellationToken);

        return JsonSerializer.SerializeToElement(new
        {
            operation = "обновить_память",
            updated = merged.UpdatedFields
        });
    }

    private static JsonElement NormalizeMemoryPatch(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new RpgValidationException("payload update_memory должен быть JSON object.");
        }

        var patch = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in payload.EnumerateObject())
        {
            patch[property.Name] = property.Value.Clone();
        }

        if (payload.TryGetProperty("summary", out var summary) && summary.ValueKind == JsonValueKind.String)
        {
            patch["summaryAppend"] = summary.Clone();
        }

        if (payload.TryGetProperty("facts", out var facts) && facts.ValueKind == JsonValueKind.Array)
        {
            patch["importantFactsAdd"] = facts.Clone();
        }

        if (payload.TryGetProperty("scene", out var scene) && scene.ValueKind == JsonValueKind.Object)
        {
            patch["currentScene"] = scene.Clone();
        }

        return JsonSerializer.SerializeToElement(patch);
    }

    private static Task<JsonElement> UpdateSceneAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        var scene = GetOptionalElement(payload, "scene", "сцена", "currentScene", "текущаяСцена") ?? payload;
        if (scene.ValueKind != JsonValueKind.Object)
        {
            throw new RpgValidationException("scene должен быть JSON object.");
        }

        var patch = JsonSerializer.SerializeToElement(new
        {
            currentScene = scene
        });

        return UpdateMemoryAsync(connection, transaction, gameStateId, patch, cancellationToken);
    }

    private static async Task<JsonElement> MovePartyToLocationAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        var targetLocationId = GetRequiredGuid(payload, "targetLocationId", "locationId", "локацияId", "целеваяЛокацияId");
        await ValidateEntityAsync(connection, transaction, "game.locations", gameStateId, targetLocationId, "Location was not found.", cancellationToken);

        const string sql = """
            UPDATE game.game_states
            SET current_location_id = @targetLocationId,
                updated_at = now()
            WHERE id = @gameStateId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("targetLocationId", targetLocationId);
        await command.ExecuteNonQueryAsync(cancellationToken);

        await AddTravelLogAsync(connection, transaction, gameStateId, $"Game change moved party to location {targetLocationId}.", cancellationToken);
        return JsonSerializer.SerializeToElement(new { operation = "move_party_to_location", targetLocationId });
    }

    private static async Task<JsonElement> SetLocationExitLockedAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        JsonElement payload,
        bool isLocked,
        CancellationToken cancellationToken)
    {
        var exitId = GetRequiredGuid(payload, "exitId", "locationExitId", "выходId");
        const string sql = """
            UPDATE game.location_exits
            SET is_locked = @isLocked
            WHERE game_state_id = @gameStateId
              AND id = @exitId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("exitId", exitId);
        command.Parameters.AddWithValue("isLocked", isLocked);
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new RpgValidationException("Location exit was not found.");
        }

        await AddTravelLogAsync(connection, transaction, gameStateId, isLocked ? $"Location exit {exitId} locked." : $"Location exit {exitId} unlocked.", cancellationToken);
        return JsonSerializer.SerializeToElement(new { operation = isLocked ? "lock_location_exit" : "unlock_location_exit", exitId, isLocked });
    }

    private static async Task AddTravelLogAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, string text, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.game_log_entries (game_state_id, turn_number, type, text, important)
            SELECT @gameStateId, COALESCE(gs.turn_number, 0), 'travel', @text, false
            FROM game.game_states gs
            WHERE gs.id = @gameStateId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("text", text);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<JsonElement> CreateQuestAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.quests (game_state_id, title, description, status)
            VALUES (@gameStateId, @title, @description, COALESCE(@status, 'active'))
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("title", GetRequiredString(payload, "title", "name", "название"));
        command.Parameters.AddWithValue("description", DbString(GetOptionalString(payload, "description", "описание")));
        command.Parameters.AddWithValue("status", DbString(GetOptionalString(payload, "status", "статус")));
        var questId = (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Quest id was not returned."));
        return JsonSerializer.SerializeToElement(new { operation = "create_quest", questId });
    }

    private static async Task<JsonElement> CreateQuestStepAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        var questId = GetRequiredGuid(payload, "questId", "quest_id", "квестId");
        await ValidateEntityAsync(connection, transaction, "game.quests", gameStateId, questId, "Quest was not found.", cancellationToken);

        const string sql = """
            INSERT INTO game.quest_steps (game_state_id, quest_id, description, sort_order)
            VALUES (@gameStateId, @questId, @description, @sortOrder)
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("questId", questId);
        command.Parameters.AddWithValue("description", GetRequiredString(payload, "description", "описание"));
        command.Parameters.AddWithValue("sortOrder", Math.Max(0, GetOptionalInt(payload, "sortOrder", "sort_order", "порядок") ?? 0));
        var questStepId = (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Quest step id was not returned."));
        return JsonSerializer.SerializeToElement(new { operation = "create_quest_step", questId, questStepId });
    }

    private static async Task<JsonElement> CompleteQuestStepAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        var questStepId = GetRequiredGuid(payload, "questStepId", "quest_step_id", "шагКвестаId");
        var questId = GetOptionalGuid(payload, "questId", "quest_id", "квестId");

        var sql = questId.HasValue
            ? "UPDATE game.quest_steps SET is_completed = true WHERE game_state_id = @gameStateId AND id = @questStepId AND quest_id = @questId;"
            : "UPDATE game.quest_steps SET is_completed = true WHERE game_state_id = @gameStateId AND id = @questStepId;";

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("questStepId", questStepId);
        if (questId.HasValue)
        {
            command.Parameters.AddWithValue("questId", questId.Value);
        }

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new RpgValidationException("Quest step was not found.");
        }

        return JsonSerializer.SerializeToElement(new { operation = "complete_quest_step", questId, questStepId });
    }

    private static async Task<JsonElement> CreateLocationAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.locations (game_state_id, name, description)
            VALUES (@gameStateId, @name, @description)
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("name", GetRequiredString(payload, "name", "название"));
        command.Parameters.AddWithValue("description", DbString(GetOptionalString(payload, "description", "описание")));
        var locationId = (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Location id was not returned."));
        return JsonSerializer.SerializeToElement(new { operation = "create_location", locationId });
    }

    private static async Task<JsonElement> UpdateLocationAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        var locationId = GetRequiredGuid(payload, "locationId", "location_id", "локацияId");
        const string sql = """
            UPDATE game.locations
            SET name = COALESCE(@name, name),
                description = COALESCE(@description, description)
            WHERE game_state_id = @gameStateId
              AND id = @locationId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("locationId", locationId);
        command.Parameters.AddWithValue("name", DbString(GetOptionalString(payload, "name", "название")));
        command.Parameters.AddWithValue("description", DbString(GetOptionalString(payload, "description", "описание")));
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new RpgValidationException("Location was not found.");
        }

        return JsonSerializer.SerializeToElement(new { operation = "update_location", locationId });
    }

    private static async Task<JsonElement> CreateNpcAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        var locationId = GetOptionalGuid(payload, "locationId", "location_id", "локацияId");
        if (locationId.HasValue)
        {
            await ValidateEntityAsync(connection, transaction, "game.locations", gameStateId, locationId.Value, "Location was not found.", cancellationToken);
        }

        const string sql = """
            INSERT INTO game.npcs (game_state_id, location_id, name, role, attitude, description, is_alive)
            VALUES (@gameStateId, @locationId, @name, @role, COALESCE(@attitude, 'neutral'), @description, COALESCE(@isAlive, true))
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("locationId", locationId.HasValue ? locationId.Value : DBNull.Value);
        command.Parameters.AddWithValue("name", GetRequiredString(payload, "name", "название"));
        command.Parameters.AddWithValue("role", DbString(GetOptionalString(payload, "role", "роль")));
        command.Parameters.AddWithValue("attitude", DbString(GetOptionalString(payload, "attitude", "отношение")));
        command.Parameters.AddWithValue("description", DbString(GetOptionalString(payload, "description", "описание")));
        command.Parameters.AddWithValue("isAlive", GetOptionalBool(payload, "isAlive", "is_alive", "живой") ?? true);
        var npcId = (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("NPC id was not returned."));
        return JsonSerializer.SerializeToElement(new { operation = "create_npc", npcId, locationId });
    }

    private static async Task<JsonElement> UpdateNpcAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        var npcId = GetRequiredGuid(payload, "npcId", "npc_id");
        var locationId = GetOptionalGuid(payload, "locationId", "location_id", "локацияId");
        if (locationId.HasValue)
        {
            await ValidateEntityAsync(connection, transaction, "game.locations", gameStateId, locationId.Value, "Location was not found.", cancellationToken);
        }

        const string sql = """
            UPDATE game.npcs
            SET location_id = COALESCE(@locationId, location_id),
                name = COALESCE(@name, name),
                role = COALESCE(@role, role),
                attitude = COALESCE(@attitude, attitude),
                description = COALESCE(@description, description),
                is_alive = COALESCE(@isAlive, is_alive)
            WHERE game_state_id = @gameStateId
              AND id = @npcId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("npcId", npcId);
        command.Parameters.AddWithValue("locationId", locationId.HasValue ? locationId.Value : DBNull.Value);
        command.Parameters.AddWithValue("name", DbString(GetOptionalString(payload, "name", "название")));
        command.Parameters.AddWithValue("role", DbString(GetOptionalString(payload, "role", "роль")));
        command.Parameters.AddWithValue("attitude", DbString(GetOptionalString(payload, "attitude", "отношение")));
        command.Parameters.AddWithValue("description", DbString(GetOptionalString(payload, "description", "описание")));
        command.Parameters.AddWithValue("isAlive", DbBool(GetOptionalBool(payload, "isAlive", "is_alive", "живой")));
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new RpgValidationException("NPC was not found.");
        }

        return JsonSerializer.SerializeToElement(new { operation = "update_npc", npcId });
    }

    private static async Task<JsonElement> CreateWorldObjectAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        var locationId = GetRequiredGuid(payload, "locationId", "location_id", "локацияId");
        await ValidateEntityAsync(connection, transaction, "game.locations", gameStateId, locationId, "Location was not found.", cancellationToken);

        const string sql = """
            INSERT INTO game.world_objects (game_state_id, location_id, name, object_type, description, state, tags)
            VALUES (@gameStateId, @locationId, @name, @objectType, @description, COALESCE(@state, 'обычное'), @tags)
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("locationId", locationId);
        command.Parameters.AddWithValue("name", GetRequiredString(payload, "name", "название"));
        command.Parameters.AddWithValue("objectType", GetRequiredString(payload, "objectType", "object_type", "type", "тип"));
        command.Parameters.AddWithValue("description", DbString(GetOptionalString(payload, "description", "описание")));
        command.Parameters.AddWithValue("state", DbString(GetOptionalString(payload, "state", "состояние")));
        AddJsonb(command, "tags", GetOptionalElement(payload, "tags", "теги")?.GetRawText() ?? "[]");
        var objectId = (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("World object id was not returned."));
        return JsonSerializer.SerializeToElement(new { operation = "create_world_object", objectId, locationId });
    }

    private static async Task<JsonElement> UpdateWorldObjectAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        var objectId = GetRequiredGuid(payload, "objectId", "object_id", "объектId");
        var locationId = GetOptionalGuid(payload, "locationId", "location_id", "локацияId");
        if (locationId.HasValue)
        {
            await ValidateEntityAsync(connection, transaction, "game.locations", gameStateId, locationId.Value, "Location was not found.", cancellationToken);
        }

        const string sql = """
            UPDATE game.world_objects
            SET location_id = COALESCE(@locationId, location_id),
                name = COALESCE(@name, name),
                object_type = COALESCE(@objectType, object_type),
                description = COALESCE(@description, description),
                state = COALESCE(@state, state),
                tags = COALESCE(@tags, tags)
            WHERE game_state_id = @gameStateId
              AND id = @objectId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("objectId", objectId);
        command.Parameters.AddWithValue("locationId", locationId.HasValue ? locationId.Value : DBNull.Value);
        command.Parameters.AddWithValue("name", DbString(GetOptionalString(payload, "name", "название")));
        command.Parameters.AddWithValue("objectType", DbString(GetOptionalString(payload, "objectType", "object_type", "type", "тип")));
        command.Parameters.AddWithValue("description", DbString(GetOptionalString(payload, "description", "описание")));
        command.Parameters.AddWithValue("state", DbString(GetOptionalString(payload, "state", "состояние")));
        AddJsonb(command, "tags", GetOptionalElement(payload, "tags", "теги")?.GetRawText());
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new RpgValidationException("World object was not found.");
        }

        return JsonSerializer.SerializeToElement(new { operation = "update_world_object", objectId });
    }

    private static async Task<JsonElement> AddItemAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        var characterId = GetOptionalGuid(payload, "characterId", "character_id", "playerId", "player_id", "персонажId");
        var ownerKind = GetOptionalString(payload, "ownerKind", "owner_kind") ?? (characterId.HasValue ? "player_inventory" : null);
        var ownerId = GetOptionalGuid(payload, "ownerId", "owner_id") ?? characterId;

        if (string.IsNullOrWhiteSpace(ownerKind) || !ownerId.HasValue)
        {
            throw new RpgValidationException("Item ownerKind/ownerId or characterId is required.");
        }

        await ValidateOwnerAsync(connection, transaction, gameStateId, ownerKind, ownerId.Value, cancellationToken);

        const string sql = """
            INSERT INTO game.item_instances
            (
                game_state_id,
                template_id,
                name,
                item_type,
                subtype,
                description,
                quantity,
                stackable,
                weight_each,
                condition,
                rarity,
                is_magical,
                price_copper,
                price_silver,
                price_gold,
                price_platinum,
                tags,
                owner_kind,
                owner_id
            )
            VALUES
            (
                @gameStateId,
                @templateId,
                @name,
                @itemType,
                @subtype,
                @description,
                @quantity,
                @stackable,
                @weightEach,
                @condition,
                @rarity,
                @isMagical,
                @priceCopper,
                @priceSilver,
                @priceGold,
                @pricePlatinum,
                @tags,
                @ownerKind,
                @ownerId
            )
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("templateId", DbString(GetOptionalString(payload, "templateId", "template_id")));
        command.Parameters.AddWithValue("name", GetRequiredString(payload, "name", "название"));
        command.Parameters.AddWithValue("itemType", GetRequiredString(payload, "itemType", "item_type", "тип"));
        command.Parameters.AddWithValue("subtype", DbString(GetOptionalString(payload, "subtype", "подтип")));
        command.Parameters.AddWithValue("description", DbString(GetOptionalString(payload, "description", "описание")));
        command.Parameters.AddWithValue("quantity", Math.Max(0, GetOptionalInt(payload, "quantity", "количество") ?? 1));
        command.Parameters.AddWithValue("stackable", GetOptionalBool(payload, "stackable", "стакуемый") ?? false);
        command.Parameters.AddWithValue("weightEach", Math.Max(0m, GetOptionalDecimal(payload, "weightEach", "weight_each", "вес") ?? 0m));
        command.Parameters.AddWithValue("condition", GetOptionalString(payload, "condition", "состояние") ?? "normal");
        command.Parameters.AddWithValue("rarity", GetOptionalString(payload, "rarity", "редкость") ?? "common");
        command.Parameters.AddWithValue("isMagical", GetOptionalBool(payload, "isMagical", "is_magical", "магический") ?? false);
        command.Parameters.AddWithValue("priceCopper", Math.Max(0, GetOptionalInt(payload, "priceCopper", "price_copper", "медные") ?? 0));
        command.Parameters.AddWithValue("priceSilver", Math.Max(0, GetOptionalInt(payload, "priceSilver", "price_silver", "серебряные") ?? 0));
        command.Parameters.AddWithValue("priceGold", Math.Max(0, GetOptionalInt(payload, "priceGold", "price_gold", "золотые") ?? 0));
        command.Parameters.AddWithValue("pricePlatinum", Math.Max(0, GetOptionalInt(payload, "pricePlatinum", "price_platinum", "платиновые") ?? 0));
        AddJsonb(command, "tags", GetOptionalElement(payload, "tags", "теги")?.GetRawText() ?? "[]");
        command.Parameters.AddWithValue("ownerKind", ownerKind);
        command.Parameters.AddWithValue("ownerId", ownerId.Value);

        var itemId = (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Item id was not returned."));

        return JsonSerializer.SerializeToElement(new { operation = "добавить_предмет", itemId });
    }

    private static async Task<JsonElement> ChangeHpAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        var characterId = GetRequiredGuid(payload, "characterId", "character_id", "playerId", "player_id");
        await ValidateCharacterAsync(connection, transaction, gameStateId, characterId, cancellationToken);

        var hpMax = GetOptionalInt(payload, "hpMax", "hp_max", "хпМаксимум");
        var hpCurrent = GetOptionalInt(payload, "hpCurrent", "hp_current", "хпТекущее");
        var delta = GetOptionalInt(payload, "delta", "изменение");
        if (!hpMax.HasValue && !hpCurrent.HasValue && !delta.HasValue)
        {
            throw new RpgValidationException("hpCurrent, hpMax, or delta is required.");
        }

        const string sql = """
            UPDATE game.player_resources
            SET hp_max = CASE WHEN @hpMax IS NULL THEN hp_max ELSE GREATEST(0, @hpMax) END,
                hp_current = CASE
                    WHEN @hpCurrent IS NOT NULL THEN GREATEST(0, @hpCurrent)
                    WHEN @delta IS NOT NULL THEN GREATEST(0, hp_current + @delta)
                    ELSE hp_current
                END
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("hpMax", hpMax.HasValue ? hpMax.Value : DBNull.Value);
        command.Parameters.AddWithValue("hpCurrent", hpCurrent.HasValue ? hpCurrent.Value : DBNull.Value);
        command.Parameters.AddWithValue("delta", delta.HasValue ? delta.Value : DBNull.Value);
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new RpgValidationException("Player resources were not found.");
        }

        return JsonSerializer.SerializeToElement(new { operation = "изменить_хп", characterId });
    }

    private static async Task<JsonElement> ChangeResourceAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        var resourceId = GetOptionalGuid(payload, "resourceId", "resource_id");
        if (resourceId.HasValue)
        {
            const string limitedSql = """
                UPDATE game.limited_resources
                SET current_value = CASE
                        WHEN @currentValue IS NOT NULL THEN GREATEST(0, @currentValue)
                        WHEN @delta IS NOT NULL THEN GREATEST(0, current_value + @delta)
                        ELSE current_value
                    END,
                    max_value = CASE WHEN @maxValue IS NULL THEN max_value ELSE GREATEST(0, @maxValue) END
                WHERE game_state_id = @gameStateId
                  AND id = @resourceId;
            """;

            await using var command = new NpgsqlCommand(limitedSql, connection, transaction);
            command.Parameters.AddWithValue("gameStateId", gameStateId);
            command.Parameters.AddWithValue("resourceId", resourceId.Value);
            command.Parameters.AddWithValue("currentValue", DbInt(GetOptionalInt(payload, "currentValue", "current_value", "текущее")));
            command.Parameters.AddWithValue("maxValue", DbInt(GetOptionalInt(payload, "maxValue", "max_value", "максимум")));
            command.Parameters.AddWithValue("delta", DbInt(GetOptionalInt(payload, "delta", "изменение")));
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                throw new RpgValidationException("Limited resource was not found.");
            }

            return JsonSerializer.SerializeToElement(new { operation = "изменить_ресурс", resourceId });
        }

        var characterId = GetRequiredGuid(payload, "characterId", "character_id", "playerId", "player_id");
        await ValidateCharacterAsync(connection, transaction, gameStateId, characterId, cancellationToken);

        const string sql = """
            UPDATE game.player_resources
            SET mana_current = CASE WHEN @manaCurrent IS NULL THEN mana_current ELSE GREATEST(0, @manaCurrent) END,
                mana_max = CASE WHEN @manaMax IS NULL THEN mana_max ELSE GREATEST(0, @manaMax) END,
                action_points_current = CASE WHEN @actionPointsCurrent IS NULL THEN action_points_current ELSE GREATEST(0, @actionPointsCurrent) END,
                action_points_max = CASE WHEN @actionPointsMax IS NULL THEN action_points_max ELSE GREATEST(0, @actionPointsMax) END
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId;
        """;

        await using var update = new NpgsqlCommand(sql, connection, transaction);
        update.Parameters.AddWithValue("gameStateId", gameStateId);
        update.Parameters.AddWithValue("characterId", characterId);
        update.Parameters.AddWithValue("manaCurrent", DbInt(GetOptionalInt(payload, "manaCurrent", "mana_current", "манаТекущая")));
        update.Parameters.AddWithValue("manaMax", DbInt(GetOptionalInt(payload, "manaMax", "mana_max", "манаМаксимум")));
        update.Parameters.AddWithValue("actionPointsCurrent", DbInt(GetOptionalInt(payload, "actionPointsCurrent", "action_points_current")));
        update.Parameters.AddWithValue("actionPointsMax", DbInt(GetOptionalInt(payload, "actionPointsMax", "action_points_max")));
        if (await update.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new RpgValidationException("Player resources were not found.");
        }

        return JsonSerializer.SerializeToElement(new { operation = "изменить_ресурс", characterId });
    }

    private static async Task<JsonElement> AddConditionAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        var characterId = GetRequiredGuid(payload, "characterId", "character_id", "playerId", "player_id");
        await ValidateCharacterAsync(connection, transaction, gameStateId, characterId, cancellationToken);

        const string sql = """
            INSERT INTO game.conditions
            (
                game_state_id,
                player_id,
                name,
                type,
                description,
                source,
                remaining_turns,
                is_permanent,
                stacks,
                max_stacks,
                effects,
                tags
            )
            VALUES
            (
                @gameStateId,
                @characterId,
                @name,
                @type,
                @description,
                @source,
                @remainingTurns,
                @isPermanent,
                @stacks,
                @maxStacks,
                @effects,
                @tags
            )
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("name", GetRequiredString(payload, "name", "название"));
        command.Parameters.AddWithValue("type", GetRequiredString(payload, "type", "тип"));
        command.Parameters.AddWithValue("description", DbString(GetOptionalString(payload, "description", "описание")));
        command.Parameters.AddWithValue("source", DbString(GetOptionalString(payload, "source", "источник")));
        command.Parameters.AddWithValue("remainingTurns", DbInt(GetOptionalInt(payload, "remainingTurns", "remaining_turns")));
        command.Parameters.AddWithValue("isPermanent", GetOptionalBool(payload, "isPermanent", "is_permanent") ?? false);
        command.Parameters.AddWithValue("stacks", Math.Max(1, GetOptionalInt(payload, "stacks", "стаки") ?? 1));
        command.Parameters.AddWithValue("maxStacks", DbInt(GetOptionalInt(payload, "maxStacks", "max_stacks")));
        AddJsonb(command, "effects", GetOptionalElement(payload, "effects", "эффекты")?.GetRawText() ?? "[]");
        AddJsonb(command, "tags", GetOptionalElement(payload, "tags", "теги")?.GetRawText() ?? "[]");

        var conditionId = (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Condition id was not returned."));

        return JsonSerializer.SerializeToElement(new { operation = "добавить_состояние", conditionId });
    }

    private static async Task<JsonElement> DeleteConditionAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        var conditionId = GetRequiredGuid(payload, "conditionId", "condition_id");
        const string sql = """
            DELETE FROM game.conditions
            WHERE game_state_id = @gameStateId
              AND id = @conditionId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("conditionId", conditionId);
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new RpgValidationException("Condition was not found.");
        }

        return JsonSerializer.SerializeToElement(new { operation = "удалить_состояние", conditionId });
    }

    private static async Task<JsonElement> UpdateQuestAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        var questId = GetRequiredGuid(payload, "questId", "quest_id");
        const string sql = """
            UPDATE game.quests
            SET title = COALESCE(@title, title),
                description = COALESCE(@description, description),
                status = COALESCE(@status, status),
                reward_experience = CASE WHEN @rewardExperience IS NULL THEN reward_experience ELSE GREATEST(0, @rewardExperience) END,
                reward_copper = CASE WHEN @rewardCopper IS NULL THEN reward_copper ELSE GREATEST(0, @rewardCopper) END,
                reward_silver = CASE WHEN @rewardSilver IS NULL THEN reward_silver ELSE GREATEST(0, @rewardSilver) END,
                reward_gold = CASE WHEN @rewardGold IS NULL THEN reward_gold ELSE GREATEST(0, @rewardGold) END,
                reward_platinum = CASE WHEN @rewardPlatinum IS NULL THEN reward_platinum ELSE GREATEST(0, @rewardPlatinum) END
            WHERE game_state_id = @gameStateId
              AND id = @questId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("questId", questId);
        command.Parameters.AddWithValue("title", DbString(GetOptionalString(payload, "title", "название")));
        command.Parameters.AddWithValue("description", DbString(GetOptionalString(payload, "description", "описание")));
        command.Parameters.AddWithValue("status", DbString(GetOptionalString(payload, "status", "статус")));
        command.Parameters.AddWithValue("rewardExperience", DbInt(GetOptionalInt(payload, "rewardExperience", "reward_experience")));
        command.Parameters.AddWithValue("rewardCopper", DbInt(GetOptionalInt(payload, "rewardCopper", "reward_copper")));
        command.Parameters.AddWithValue("rewardSilver", DbInt(GetOptionalInt(payload, "rewardSilver", "reward_silver")));
        command.Parameters.AddWithValue("rewardGold", DbInt(GetOptionalInt(payload, "rewardGold", "reward_gold")));
        command.Parameters.AddWithValue("rewardPlatinum", DbInt(GetOptionalInt(payload, "rewardPlatinum", "reward_platinum")));
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new RpgValidationException("Quest was not found.");
        }

        return JsonSerializer.SerializeToElement(new { operation = "обновить_квест", questId });
    }

    private static async Task<JsonElement> AddLogEntryAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        var turnNumber = GetOptionalInt(payload, "turnNumber", "turn_number");
        if (!turnNumber.HasValue)
        {
            const string turnSql = "SELECT turn_number FROM game.game_states WHERE id = @gameStateId;";
            await using var turnCommand = new NpgsqlCommand(turnSql, connection, transaction);
            turnCommand.Parameters.AddWithValue("gameStateId", gameStateId);
            turnNumber = Convert.ToInt32(await turnCommand.ExecuteScalarAsync(cancellationToken) ?? 0);
        }

        const string sql = """
            INSERT INTO game.game_log_entries
            (
                game_state_id,
                turn_number,
                type,
                text,
                important
            )
            VALUES
            (
                @gameStateId,
                @turnNumber,
                @type,
                @text,
                @important
            )
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("turnNumber", Math.Max(0, turnNumber.Value));
        command.Parameters.AddWithValue("type", GetOptionalString(payload, "type", "тип") ?? "system");
        command.Parameters.AddWithValue("text", GetRequiredString(payload, "text", "текст"));
        command.Parameters.AddWithValue("important", GetOptionalBool(payload, "important", "важное") ?? false);
        var logId = (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Log entry id was not returned."));

        return JsonSerializer.SerializeToElement(new { operation = "добавить_запись_журнала", logId });
    }

    private static async Task<JsonElement> MoveItemAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        var itemId = GetRequiredGuid(payload, "itemId", "item_id");
        var ownerKind = GetRequiredString(payload, "ownerKind", "owner_kind");
        var ownerId = GetRequiredGuid(payload, "ownerId", "owner_id");
        await ValidateOwnerAsync(connection, transaction, gameStateId, ownerKind, ownerId, cancellationToken);

        const string sql = """
            UPDATE game.item_instances
            SET owner_kind = @ownerKind,
                owner_id = @ownerId
            WHERE game_state_id = @gameStateId
              AND id = @itemId;
        """;

        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        {
            command.Parameters.AddWithValue("gameStateId", gameStateId);
            command.Parameters.AddWithValue("itemId", itemId);
            command.Parameters.AddWithValue("ownerKind", ownerKind);
            command.Parameters.AddWithValue("ownerId", ownerId);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                throw new RpgValidationException("Item was not found.");
            }
        }

        const string clearEquipmentSql = """
            UPDATE game.equipped_gear
            SET head_item_id = NULLIF(head_item_id, @itemId),
                body_item_id = NULLIF(body_item_id, @itemId),
                hands_item_id = NULLIF(hands_item_id, @itemId),
                legs_item_id = NULLIF(legs_item_id, @itemId),
                feet_item_id = NULLIF(feet_item_id, @itemId),
                main_hand_item_id = NULLIF(main_hand_item_id, @itemId),
                off_hand_item_id = NULLIF(off_hand_item_id, @itemId),
                amulet_item_id = NULLIF(amulet_item_id, @itemId),
                ring1_item_id = NULLIF(ring1_item_id, @itemId),
                ring2_item_id = NULLIF(ring2_item_id, @itemId)
            WHERE game_state_id = @gameStateId;
        """;

        await using (var clearCommand = new NpgsqlCommand(clearEquipmentSql, connection, transaction))
        {
            clearCommand.Parameters.AddWithValue("gameStateId", gameStateId);
            clearCommand.Parameters.AddWithValue("itemId", itemId);
            await clearCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        return JsonSerializer.SerializeToElement(new { operation = "переместить_предмет", itemId, ownerKind, ownerId });
    }

    private static async Task MarkAppliedAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid accountId,
        Guid gameStateId,
        Guid changeId,
        JsonElement applyResult,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE game.game_changes
            SET status = 'applied',
                processed_at = now(),
                processed_by_account_id = @accountId,
                apply_result = @applyResult,
                error_message = NULL
            WHERE game_state_id = @gameStateId
              AND id = @changeId
              AND status = 'pending';
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("changeId", changeId);
        AddJsonb(command, "applyResult", applyResult.GetRawText());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<PendingChange?> GetPendingChangeForUpdateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid accountId,
        Guid gameStateId,
        Guid changeId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT c.operation, c.payload::text, c.game_turn_id
            FROM game.game_changes c
            JOIN game.game_states gs ON gs.id = c.game_state_id
            WHERE gs.account_id = @accountId
              AND c.game_state_id = @gameStateId
              AND c.id = @changeId
              AND c.status = 'pending'
            FOR UPDATE OF c;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("changeId", changeId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new PendingChange(
            reader.GetString(0),
            RpgDbJson.ParseElement(reader.GetString(1)),
            reader.IsDBNull(2) ? null : reader.GetGuid(2));
    }

    private static async Task<JsonElement> SelectMemoryForUpdateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT jsonb_build_object(
                'gameStateId', cm.game_state_id,
                'резюме', cm.summary,
                'текущаяСцена', cm.current_scene,
                'важныеФакты', cm.important_facts,
                'открытыеЛинии', cm.open_threads,
                'закрытыеЛинии', cm.resolved_threads,
                'известныеNpc', cm.known_npcs,
                'известныеЛокации', cm.known_locations,
                'секретыМастера', cm.master_secrets,
                'updatedAt', cm.updated_at
            )::text
            FROM game.campaign_memories cm
            WHERE cm.game_state_id = @gameStateId
            FOR UPDATE;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        if (value is null or DBNull)
        {
            throw new RpgValidationException("Память кампании не найдена.");
        }

        return RpgDbJson.ParseElement(value.ToString()!);
    }

    private static async Task<JsonElement?> GetChangeAsync(NpgsqlConnection connection, Guid accountId, Guid gameStateId, Guid changeId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT jsonb_build_object(
                'id', c.id,
                'gameStateId', c.game_state_id,
                'turnId', c.game_turn_id,
                'operation', c.operation,
                'payload', c.payload,
                'status', c.status,
                'rejectReason', c.reject_reason,
                'errorMessage', c.error_message,
                'applyResult', c.apply_result,
                'createdAt', c.created_at,
                'processedAt', c.processed_at,
                'processedByAccountId', c.processed_by_account_id
            )::text
            FROM game.game_changes c
            JOIN game.game_states gs ON gs.id = c.game_state_id
            WHERE gs.account_id = @accountId
              AND c.game_state_id = @gameStateId
              AND c.id = @changeId
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("changeId", changeId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    private static async Task ValidateCharacterAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM game.players WHERE id = @id AND game_state_id = @gameStateId);";
        if (!await ExistsAsync(connection, transaction, sql, gameStateId, characterId, cancellationToken))
        {
            throw new RpgValidationException("Character was not found in this GameState.");
        }
    }

    private static async Task ValidateEntityAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string tableName,
        Guid gameStateId,
        Guid id,
        string message,
        CancellationToken cancellationToken)
    {
        var sql = $"SELECT EXISTS (SELECT 1 FROM {tableName} WHERE id = @id AND game_state_id = @gameStateId);";
        if (!await ExistsAsync(connection, transaction, sql, gameStateId, id, cancellationToken))
        {
            throw new RpgValidationException(message);
        }
    }

    private static async Task ValidateOwnerAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, string ownerKind, Guid ownerId, CancellationToken cancellationToken)
    {
        var sql = ownerKind switch
        {
            "player_inventory" => "SELECT EXISTS (SELECT 1 FROM game.players WHERE id = @id AND game_state_id = @gameStateId);",
            "location" => "SELECT EXISTS (SELECT 1 FROM game.locations WHERE id = @id AND game_state_id = @gameStateId);",
            "world_object" => "SELECT EXISTS (SELECT 1 FROM game.world_objects WHERE id = @id AND game_state_id = @gameStateId);",
            "container" => "SELECT EXISTS (SELECT 1 FROM game.world_containers WHERE id = @id AND game_state_id = @gameStateId);",
            "npc" => "SELECT EXISTS (SELECT 1 FROM game.npcs WHERE id = @id AND game_state_id = @gameStateId);",
            _ => throw new RpgValidationException("Unsupported item ownerKind.")
        };

        if (!await ExistsAsync(connection, transaction, sql, gameStateId, ownerId, cancellationToken))
        {
            throw new RpgValidationException("Item owner target was not found in this GameState.");
        }
    }

    private static async Task<bool> ExistsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql, Guid gameStateId, Guid id, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task<bool> GameStateExistsAsync(NpgsqlConnection connection, Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM game.game_states
                WHERE id = @gameStateId
                  AND account_id = @accountId
            );
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static Guid GetRequiredGuid(JsonElement payload, params string[] names)
        => GetOptionalGuid(payload, names) ?? throw new RpgValidationException($"{names[0]} is required.");

    private static Guid? GetOptionalGuid(JsonElement payload, params string[] names)
    {
        var element = GetOptionalElement(payload, names);
        if (!element.HasValue || element.Value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (element.Value.ValueKind == JsonValueKind.String && Guid.TryParse(element.Value.GetString(), out var id))
        {
            return id;
        }

        throw new RpgValidationException($"{names[0]} must be a UUID.");
    }

    private static string GetRequiredString(JsonElement payload, params string[] names)
    {
        var value = GetOptionalString(payload, names);
        return string.IsNullOrWhiteSpace(value)
            ? throw new RpgValidationException($"{names[0]} is required.")
            : value.Trim();
    }

    private static string? GetOptionalString(JsonElement payload, params string[] names)
    {
        var element = GetOptionalElement(payload, names);
        return element.HasValue && element.Value.ValueKind == JsonValueKind.String
            ? element.Value.GetString()
            : null;
    }

    private static int? GetOptionalInt(JsonElement payload, params string[] names)
    {
        var element = GetOptionalElement(payload, names);
        if (!element.HasValue || element.Value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return element.Value.ValueKind switch
        {
            JsonValueKind.Number when element.Value.TryGetInt32(out var value) => value,
            JsonValueKind.String when int.TryParse(element.Value.GetString(), out var value) => value,
            _ => throw new RpgValidationException($"{names[0]} must be an integer.")
        };
    }

    private static decimal? GetOptionalDecimal(JsonElement payload, params string[] names)
    {
        var element = GetOptionalElement(payload, names);
        if (!element.HasValue || element.Value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return element.Value.ValueKind switch
        {
            JsonValueKind.Number when element.Value.TryGetDecimal(out var value) => value,
            JsonValueKind.String when decimal.TryParse(element.Value.GetString(), out var value) => value,
            _ => throw new RpgValidationException($"{names[0]} must be a number.")
        };
    }

    private static bool? GetOptionalBool(JsonElement payload, params string[] names)
    {
        var element = GetOptionalElement(payload, names);
        if (!element.HasValue || element.Value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return element.Value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(element.Value.GetString(), out var value) => value,
            _ => throw new RpgValidationException($"{names[0]} must be a boolean.")
        };
    }

    private static JsonElement? GetOptionalElement(JsonElement payload, params string[] names)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new RpgValidationException("Change payload must be a JSON object.");
        }

        foreach (var name in names)
        {
            if (payload.TryGetProperty(name, out var element))
            {
                return element;
            }
        }

        return null;
    }

    private static object DbString(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static object DbInt(int? value) => value.HasValue ? value.Value : DBNull.Value;

    private static object DbBool(bool? value) => value.HasValue ? value.Value : DBNull.Value;

    private static void AddJsonb(NpgsqlCommand command, string name, string? json)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Jsonb);
        parameter.Value = string.IsNullOrWhiteSpace(json) ? DBNull.Value : json;
    }

    private readonly record struct PendingChange(string Operation, JsonElement Payload, Guid? GameTurnId);
}
