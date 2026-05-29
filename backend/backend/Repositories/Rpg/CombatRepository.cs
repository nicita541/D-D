using System.Text.Json;
using backend.Contracts.Rpg.Combat;
using backend.Infrastructure.Database;
using backend.Services.Rpg;
using Npgsql;

namespace backend.Repositories.Rpg;

public sealed class CombatRepository : ICombatRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public CombatRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<JsonElement?> GetCombatStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await SelectCombatStateAsync(connection, null, accountId, gameStateId, cancellationToken);
    }

    public async Task<Guid?> StartCombatAsync(Guid accountId, Guid gameStateId, StartCombatRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var combatId = await EnsureCombatAsync(connection, transaction, accountId, gameStateId, isActive: true, cancellationToken);
            if (!combatId.HasValue)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await using (var delete = new NpgsqlCommand(
                "DELETE FROM game.combat_participants WHERE game_state_id = @gameStateId AND combat_state_id = @combatId;",
                connection,
                transaction))
            {
                delete.Parameters.AddWithValue("gameStateId", gameStateId);
                delete.Parameters.AddWithValue("combatId", combatId.Value);
                await delete.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var participant in request.Participants)
            {
                await InsertParticipantAsync(connection, transaction, gameStateId, combatId.Value, participant, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return combatId.Value;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> EndCombatAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE game.combat_states c
            SET is_active = false,
                updated_at = now()
            FROM game.game_states gs
            WHERE gs.id = c.game_state_id
              AND gs.account_id = @accountId
              AND c.game_state_id = @gameStateId
              AND c.is_active = true;
        """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<Guid?> AddParticipantAsync(Guid accountId, Guid gameStateId, AddCombatParticipantRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var combatId = await EnsureCombatAsync(connection, transaction, accountId, gameStateId, isActive: true, cancellationToken);
            if (!combatId.HasValue)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var participantId = await InsertParticipantAsync(connection, transaction, gameStateId, combatId.Value, request, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return participantId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<JsonElement?> NextTurnAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var combat = await GetActiveCombatForUpdateAsync(connection, transaction, accountId, gameStateId, cancellationToken);
            if (combat is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var participants = (await GetOrderedParticipantIdsForUpdateAsync(connection, transaction, combat.Value.Id, cancellationToken)).ToList();
            if (participants.Count == 0)
            {
                throw new CombatValidationException("В активном бою нет участников.");
            }

            var currentIndex = combat.Value.CurrentParticipantId.HasValue
                ? participants.IndexOf(combat.Value.CurrentParticipantId.Value)
                : -1;
            var wrapped = currentIndex == participants.Count - 1;
            var nextParticipantId = currentIndex < 0 || wrapped
                ? participants[0]
                : participants[currentIndex + 1];
            var nextRound = wrapped ? combat.Value.RoundNumber + 1 : combat.Value.RoundNumber;

            if (combat.Value.CurrentParticipantId.HasValue && currentIndex >= 0)
            {
                await SetParticipantActedAsync(connection, transaction, combat.Value.CurrentParticipantId.Value, true, cancellationToken);
            }

            if (wrapped)
            {
                await ResetRoundParticipantsAsync(connection, transaction, combat.Value.Id, cancellationToken);
            }

            const string updateSql = """
                UPDATE game.combat_states
                SET current_turn_participant_id = @participantId,
                    round_number = @roundNumber,
                    updated_at = now()
                WHERE id = @combatId
                  AND game_state_id = @gameStateId;
            """;

            await using (var update = new NpgsqlCommand(updateSql, connection, transaction))
            {
                update.Parameters.AddWithValue("combatId", combat.Value.Id);
                update.Parameters.AddWithValue("gameStateId", gameStateId);
                update.Parameters.AddWithValue("participantId", nextParticipantId);
                update.Parameters.AddWithValue("roundNumber", nextRound);
                await update.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return await GetCombatStateAsync(accountId, gameStateId, cancellationToken);
    }

    public async Task<JsonElement?> ApplyDamageAsync(Guid accountId, Guid gameStateId, ApplyCombatDamageRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var participant = await GetParticipantForUpdateAsync(connection, transaction, accountId, gameStateId, request.ResolvedTargetParticipantId!.Value, cancellationToken);
            if (participant is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var newHp = CombatRules.ApplyDamage(participant.Value.HpCurrent, request.ResolvedDamage!.Value);
            var conditions = newHp <= 0
                ? CombatRules.AddConditionOnce(participant.Value.Conditions, CombatRules.DefeatedCondition)
                : participant.Value.Conditions;

            await UpdateParticipantHpAsync(connection, transaction, participant.Value.Id, newHp, conditions, cancellationToken);
            await InsertCombatLogAsync(connection, transaction, gameStateId, $"Урон: {participant.Value.Name} получает {request.ResolvedDamage.Value} ({request.ResolvedDamageType}). {request.ResolvedReason}".Trim(), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return await GetCombatStateAsync(accountId, gameStateId, cancellationToken);
    }

    public async Task<JsonElement?> HealParticipantAsync(Guid accountId, Guid gameStateId, Guid participantId, HealCombatParticipantRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var participant = await GetParticipantForUpdateAsync(connection, transaction, accountId, gameStateId, participantId, cancellationToken);
            if (participant is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var newHp = CombatRules.ApplyHealing(participant.Value.HpCurrent, participant.Value.HpMax, request.ResolvedHealing!.Value);
            await UpdateParticipantHpAsync(connection, transaction, participant.Value.Id, newHp, participant.Value.Conditions, cancellationToken);
            await InsertCombatLogAsync(connection, transaction, gameStateId, $"Лечение: {participant.Value.Name} восстанавливает {request.ResolvedHealing.Value} HP. {request.ResolvedReason}".Trim(), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return await GetCombatStateAsync(accountId, gameStateId, cancellationToken);
    }

    public async Task<JsonElement?> ResolveAttackAsync(Guid accountId, Guid gameStateId, CombatAttackRequest request, DiceRollResult attackRoll, DiceRollResult damageRoll, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var attackerExists = await ParticipantExistsAsync(connection, transaction, accountId, gameStateId, request.ResolvedAttackerParticipantId!.Value, cancellationToken);
            var target = await GetParticipantForUpdateAsync(connection, transaction, accountId, gameStateId, request.ResolvedTargetParticipantId!.Value, cancellationToken);
            if (!attackerExists || target is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var hit = attackRoll.Total >= target.Value.ArmorClass;
            IReadOnlyList<string> conditions = target.Value.Conditions;
            var newHp = target.Value.HpCurrent;
            if (hit)
            {
                newHp = CombatRules.ApplyDamage(target.Value.HpCurrent, damageRoll.Total);
                conditions = newHp <= 0
                    ? CombatRules.AddConditionOnce(target.Value.Conditions, CombatRules.DefeatedCondition)
                    : target.Value.Conditions;
                await UpdateParticipantHpAsync(connection, transaction, target.Value.Id, newHp, conditions, cancellationToken);
            }

            await InsertCombatLogAsync(connection, transaction, gameStateId, hit
                ? $"Атака попала: {target.Value.Name} получает {damageRoll.Total} ({request.ResolvedDamageType}). {request.ResolvedReason}".Trim()
                : $"Атака промахнулась по {target.Value.Name}. {request.ResolvedReason}".Trim(), cancellationToken);

            var response = JsonSerializer.SerializeToElement(new
            {
                hit,
                armorClass = target.Value.ArmorClass,
                attackRoll = ToRollObject(attackRoll),
                damageRoll = hit ? ToRollObject(damageRoll) : null,
                targetParticipant = new
                {
                    id = target.Value.Id,
                    имя = target.Value.Name,
                    хпТекущее = newHp,
                    хпМаксимум = target.Value.HpMax,
                    состояния = conditions
                }
            });

            await transaction.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<JsonElement?> SelectCombatStateAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
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
                                'классДоспеха', p.armor_class,
                                'ужеДействовал', p.has_acted,
                                'состояния', p.conditions
                            ) ORDER BY p.initiative DESC, p.created_at ASC, p.id ASC
                        )
                        FROM game.combat_participants p
                        WHERE p.combat_state_id = c.id
                    ),
                    '[]'::jsonb
                )
            )::text
            FROM game.combat_states c
            JOIN game.game_states gs ON gs.id = c.game_state_id
            WHERE c.game_state_id = @gameStateId
              AND gs.account_id = @accountId
            LIMIT 1;
        """;

        await using var command = transaction is null
            ? new NpgsqlCommand(sql, connection)
            : new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    private static async Task<Guid?> EnsureCombatAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid accountId,
        Guid gameStateId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.combat_states
            (
                id,
                game_state_id,
                is_active,
                round_number,
                current_turn_participant_id
            )
            SELECT
                gen_random_uuid(),
                @gameStateId,
                @isActive,
                1,
                NULL
            WHERE EXISTS (
                SELECT 1
                FROM game.game_states
                WHERE id = @gameStateId
                  AND account_id = @accountId
            )
            ON CONFLICT (game_state_id)
            DO UPDATE SET
                is_active = EXCLUDED.is_active,
                round_number = 1,
                current_turn_participant_id = NULL,
                updated_at = now()
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("isActive", isActive);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : (Guid)value;
    }

    private static async Task<Guid> InsertParticipantAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        Guid combatId,
        AddCombatParticipantRequest request,
        CancellationToken cancellationToken)
    {
        var actorType = NormalizeActorType(request.ActorType);
        await ValidateParticipantActorAsync(connection, transaction, gameStateId, actorType, request.ActorId, cancellationToken);

        if (request.ResolvedArmorClass < 0)
        {
            throw new CombatValidationException("классДоспеха должен быть 0 или больше.");
        }

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
                armor_class,
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
                @armorClass,
                @conditions::jsonb
            )
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("combatId", combatId);
        command.Parameters.AddWithValue("actorType", actorType);
        command.Parameters.AddWithValue("actorId", request.ActorId);
        command.Parameters.AddWithValue("name", string.IsNullOrWhiteSpace(request.Name) ? "Безымянный" : request.Name.Trim());
        command.Parameters.AddWithValue("initiative", request.Initiative);
        command.Parameters.AddWithValue("hpCurrent", Math.Max(0, request.HpCurrent));
        command.Parameters.AddWithValue("hpMax", Math.Max(0, request.HpMax));
        command.Parameters.AddWithValue("armorClass", request.ResolvedArmorClass);
        command.Parameters.AddJsonb("conditions", request.Conditions);

        return (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Combat participant id was not returned."));
    }

    internal static string NormalizeActorType(string? actorType)
    {
        var normalized = string.IsNullOrWhiteSpace(actorType)
            ? "character"
            : actorType.Trim().ToLowerInvariant();

        if (normalized is not ("character" or "npc" or "monster"))
        {
            throw new CombatValidationException("типАктера должен быть character, npc или monster.");
        }

        return normalized;
    }

    private static async Task ValidateParticipantActorAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        string actorType,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        if (actorId == Guid.Empty)
        {
            throw new CombatValidationException("actorId обязателен.");
        }

        const string sql = """
            SELECT CASE
                WHEN @actorType = 'character' THEN EXISTS (
                    SELECT 1 FROM game.players WHERE id = @actorId AND game_state_id = @gameStateId
                )
                WHEN @actorType = 'npc' THEN EXISTS (
                    SELECT 1 FROM game.npcs WHERE id = @actorId AND game_state_id = @gameStateId
                )
                WHEN @actorType = 'monster' THEN EXISTS (
                    SELECT 1 FROM game.monsters WHERE id = @actorId AND game_state_id = @gameStateId
                )
                ELSE false
            END;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("actorType", actorType);
        command.Parameters.AddWithValue("actorId", actorId);

        if (await command.ExecuteScalarAsync(cancellationToken) is true)
        {
            return;
        }

        var message = actorType switch
        {
            "character" => "Персонаж для участника боя не найден в этом GameState.",
            "npc" => "NPC для участника боя не найден в этом GameState.",
            "monster" => "Монстр для участника боя не найден в этом GameState.",
            _ => "Актёр для участника боя не найден."
        };
        throw new CombatValidationException(message);
    }

    private static async Task<ActiveCombat?> GetActiveCombatForUpdateAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT c.id, c.current_turn_participant_id, c.round_number
            FROM game.combat_states c
            JOIN game.game_states gs ON gs.id = c.game_state_id
            WHERE gs.account_id = @accountId
              AND c.game_state_id = @gameStateId
              AND c.is_active = true
            FOR UPDATE OF c;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ActiveCombat(
            reader.GetGuid(0),
            reader.IsDBNull(1) ? null : reader.GetGuid(1),
            reader.GetInt32(2));
    }

    private static async Task<IReadOnlyList<Guid>> GetOrderedParticipantIdsForUpdateAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid combatId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id
            FROM game.combat_participants
            WHERE combat_state_id = @combatId
            ORDER BY initiative DESC, created_at ASC, id ASC
            FOR UPDATE;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("combatId", combatId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<Guid>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(reader.GetGuid(0));
        }

        return result;
    }

    private static async Task SetParticipantActedAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid participantId, bool hasActed, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("UPDATE game.combat_participants SET has_acted = @hasActed WHERE id = @participantId;", connection, transaction);
        command.Parameters.AddWithValue("participantId", participantId);
        command.Parameters.AddWithValue("hasActed", hasActed);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ResetRoundParticipantsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid combatId, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("UPDATE game.combat_participants SET has_acted = false WHERE combat_state_id = @combatId;", connection, transaction);
        command.Parameters.AddWithValue("combatId", combatId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<CombatParticipantRow?> GetParticipantForUpdateAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid accountId, Guid gameStateId, Guid participantId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT p.id, p.name, p.hp_current, p.hp_max, p.armor_class, p.conditions::text
            FROM game.combat_participants p
            JOIN game.combat_states c ON c.id = p.combat_state_id AND c.game_state_id = p.game_state_id
            JOIN game.game_states gs ON gs.id = p.game_state_id
            WHERE gs.account_id = @accountId
              AND p.game_state_id = @gameStateId
              AND p.id = @participantId
              AND c.is_active = true
            FOR UPDATE OF p;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("participantId", participantId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CombatParticipantRow(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetInt32(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            ParseConditions(reader.GetString(5)));
    }

    private static async Task<bool> ParticipantExistsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid accountId, Guid gameStateId, Guid participantId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM game.combat_participants p
                JOIN game.combat_states c ON c.id = p.combat_state_id AND c.game_state_id = p.game_state_id
                JOIN game.game_states gs ON gs.id = p.game_state_id
                WHERE gs.account_id = @accountId
                  AND p.game_state_id = @gameStateId
                  AND p.id = @participantId
                  AND c.is_active = true
            );
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("participantId", participantId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task UpdateParticipantHpAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid participantId, int hpCurrent, IReadOnlyList<string> conditions, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE game.combat_participants
            SET hp_current = @hpCurrent,
                conditions = @conditions::jsonb
            WHERE id = @participantId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("participantId", participantId);
        command.Parameters.AddWithValue("hpCurrent", hpCurrent);
        command.Parameters.AddJsonb("conditions", conditions);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertCombatLogAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, string text, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.game_log_entries (game_state_id, turn_number, type, text, important)
            SELECT @gameStateId, turn_number, 'combat', @text, false
            FROM game.game_states
            WHERE id = @gameStateId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("text", string.IsNullOrWhiteSpace(text) ? "Боевое действие." : text.Trim());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IReadOnlyList<string> ParseConditions(string json)
        => JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

    private static object ToRollObject(DiceRollResult roll)
        => new
        {
            formula = roll.Formula.Normalized,
            rolls = roll.Rolls,
            modifier = roll.Formula.Modifier,
            total = roll.Total
        };

    private readonly record struct ActiveCombat(Guid Id, Guid? CurrentParticipantId, int RoundNumber);

    private readonly record struct CombatParticipantRow(Guid Id, string Name, int HpCurrent, int HpMax, int ArmorClass, IReadOnlyList<string> Conditions);
}
