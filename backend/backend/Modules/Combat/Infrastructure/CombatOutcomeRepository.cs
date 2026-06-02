using System.Text.Json;
using backend.Infrastructure.Database;
using backend.Modules.Combat.Contracts;
using Npgsql;

namespace backend.Modules.Combat.Infrastructure;

public sealed class CombatOutcomeRepository : ICombatOutcomeRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public CombatOutcomeRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<JsonElement?> GetOutcomeAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var combat = await GetActiveCombatAsync(connection, null, accountId, gameStateId, forUpdate: false, cancellationToken);
        return combat is null ? null : BuildOutcome(combat, Array.Empty<Guid>(), resolved: false);
    }

    public async Task<JsonElement?> ResolveOutcomeAsync(Guid accountId, Guid gameStateId, PlayCombatResolveOutcomeRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var combat = await GetActiveCombatAsync(connection, transaction, accountId, gameStateId, forUpdate: true, cancellationToken);
            if (combat is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var outcome = CalculateOutcome(combat);
            var lootContainerIds = new List<Guid>();
            if (outcome == "ongoing")
            {
                await transaction.CommitAsync(cancellationToken);
                return BuildOutcome(combat, lootContainerIds, resolved: false);
            }

            await EndCombatAsync(connection, transaction, gameStateId, combat.CombatId, cancellationToken);

            if (outcome == "victory")
            {
                await MarkDefeatedMonstersAsync(connection, transaction, gameStateId, combat.DefeatedMonsterIds, cancellationToken);
                var lootContainerId = await CreateCombatLootAsync(
                    connection,
                    transaction,
                    gameStateId,
                    combat.CombatId,
                    combat.DefeatedMonsterIds,
                    request.ResolvedAutoGrantRewards ? 0 : combat.CurrencyRewardTotal,
                    cancellationToken);
                if (lootContainerId.HasValue)
                {
                    lootContainerIds.Add(lootContainerId.Value);
                }

                if (request.ResolvedAutoGrantRewards)
                {
                    foreach (var characterId in combat.CharacterIds)
                    {
                        if (combat.XpRewardTotal > 0)
                        {
                            await AddExperienceAsync(connection, transaction, gameStateId, characterId, combat.XpRewardTotal, cancellationToken);
                        }

                        if (combat.CurrencyRewardTotal > 0)
                        {
                            await AddCurrencyAsync(connection, transaction, gameStateId, characterId, combat.CurrencyRewardTotal, cancellationToken);
                        }
                    }
                }

                await InsertLogAsync(connection, transaction, gameStateId, "Победа в бою. Награды подготовлены.", cancellationToken);
            }
            else
            {
                await InsertLogAsync(connection, transaction, gameStateId, "Партия потерпела поражение в бою.", cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return BuildOutcome(combat, lootContainerIds, resolved: true);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static JsonElement BuildOutcome(CombatSnapshot combat, IReadOnlyList<Guid> lootContainerIds, bool resolved)
    {
        var status = CalculateOutcome(combat);
        var message = status switch
        {
            "victory" => "Все враги побеждены.",
            "defeat" => "Все персонажи выведены из боя.",
            _ => "Бой продолжается."
        };

        return JsonSerializer.SerializeToElement(new
        {
            status,
            combatId = combat.CombatId,
            defeatedMonsterIds = combat.DefeatedMonsterIds,
            defeatedCharacterIds = combat.DefeatedCharacterIds,
            xpRewardTotal = combat.XpRewardTotal,
            currencyRewardTotal = combat.CurrencyRewardTotal,
            lootContainerIds,
            resolved,
            message
        });
    }

    private static string CalculateOutcome(CombatSnapshot combat)
    {
        if (combat.EnemyCount > 0 && combat.DefeatedEnemyCount == combat.EnemyCount)
        {
            return "victory";
        }

        if (combat.CharacterCount > 0 && combat.DefeatedCharacterCount == combat.CharacterCount)
        {
            return "defeat";
        }

        return "ongoing";
    }

    private static async Task<CombatSnapshot?> GetActiveCombatAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid accountId, Guid gameStateId, bool forUpdate, CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT c.id,
                   p.id,
                   p.actor_type,
                   p.actor_id,
                   p.hp_current,
                   p.conditions::text,
                   p.is_enemy,
                   COALESCE(NULLIF(p.xp_reward, 0), CASE WHEN p.actor_type = 'monster' THEN COALESCE(m.xp_reward, 0) ELSE 0 END),
                   COALESCE(NULLIF(p.currency_reward, 0), CASE WHEN p.actor_type = 'monster' THEN COALESCE(m.currency_reward, 0) ELSE 0 END)
            FROM game.combat_states c
            JOIN game.game_states gs ON gs.id = c.game_state_id
            LEFT JOIN game.combat_participants p ON p.combat_state_id = c.id AND p.game_state_id = c.game_state_id
            LEFT JOIN game.monsters m ON m.id = p.actor_id AND m.game_state_id = p.game_state_id AND p.actor_type = 'monster'
            WHERE gs.account_id = @accountId
              AND c.game_state_id = @gameStateId
              AND c.is_active = true
            ORDER BY p.created_at ASC, p.id ASC
            {(forUpdate ? "FOR UPDATE OF c" : string.Empty)};
        """;

        await using var command = transaction is null
            ? new NpgsqlCommand(sql, connection)
            : new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        Guid? combatId = null;
        var participants = new List<ParticipantSnapshot>();
        while (await reader.ReadAsync(cancellationToken))
        {
            combatId ??= reader.GetGuid(0);
            if (reader.IsDBNull(1))
            {
                continue;
            }

            participants.Add(new ParticipantSnapshot(
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetGuid(3),
                reader.GetInt32(4),
                ParseConditions(reader.GetString(5)),
                reader.GetBoolean(6),
                reader.GetInt32(7),
                reader.GetInt32(8)));
        }

        return combatId.HasValue ? CombatSnapshot.From(combatId.Value, participants) : null;
    }

    private static async Task EndCombatAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid combatId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE game.combat_states
            SET is_active = false,
                updated_at = now()
            WHERE game_state_id = @gameStateId
              AND id = @combatId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("combatId", combatId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MarkDefeatedMonstersAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, IReadOnlyList<Guid> monsterIds, CancellationToken cancellationToken)
    {
        if (monsterIds.Count == 0)
        {
            return;
        }

        const string sql = """
            UPDATE game.monsters
            SET hp_current = 0,
                is_alive = false,
                status = 'dead',
                updated_at = now()
            WHERE game_state_id = @gameStateId
              AND id = ANY(@monsterIds);
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("monsterIds", monsterIds.ToArray());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Guid?> CreateCombatLootAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid combatId, IReadOnlyList<Guid> monsterIds, int currencyAmount, CancellationToken cancellationToken)
    {
        if (monsterIds.Count == 0 && currencyAmount <= 0)
        {
            return null;
        }

        const string insertContainerSql = """
            INSERT INTO game.loot_containers (game_state_id, source_type, source_id, name, currency_amount)
            VALUES (@gameStateId, 'combat', @combatId, 'Добыча после боя', @currencyAmount)
            RETURNING id;
        """;

        Guid lootContainerId;
        await using (var command = new NpgsqlCommand(insertContainerSql, connection, transaction))
        {
            command.Parameters.AddWithValue("gameStateId", gameStateId);
            command.Parameters.AddWithValue("combatId", combatId);
            command.Parameters.AddWithValue("currencyAmount", Math.Max(0, currencyAmount));
            lootContainerId = (Guid)(await command.ExecuteScalarAsync(cancellationToken)
                ?? throw new InvalidOperationException("Loot container id was not returned."));
        }

        const string insertItemsSql = """
            INSERT INTO game.loot_items (game_state_id, loot_container_id, name, description, quantity, item_type, rarity, metadata)
            SELECT @gameStateId,
                   @lootContainerId,
                   COALESCE(item->>'name', item->>'название', 'Добыча'),
                   COALESCE(item->>'description', item->>'описание'),
                   GREATEST(1, COALESCE((item->>'quantity')::int, (item->>'количество')::int, 1)),
                   COALESCE(item->>'itemType', item->>'тип', 'misc'),
                   COALESCE(item->>'rarity', item->>'редкость', 'common'),
                   item
            FROM game.monsters m
            CROSS JOIN LATERAL jsonb_array_elements(COALESCE(m.loot, '[]'::jsonb)) item
            WHERE m.game_state_id = @gameStateId
              AND m.id = ANY(@monsterIds);
        """;

        await using (var items = new NpgsqlCommand(insertItemsSql, connection, transaction))
        {
            items.Parameters.AddWithValue("gameStateId", gameStateId);
            items.Parameters.AddWithValue("lootContainerId", lootContainerId);
            items.Parameters.AddWithValue("monsterIds", monsterIds.ToArray());
            await items.ExecuteNonQueryAsync(cancellationToken);
        }

        return lootContainerId;
    }

    private static async Task AddExperienceAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid characterId, int amount, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE game.player_progression
            SET experience = experience + @amount
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("amount", Math.Max(0, amount));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task AddCurrencyAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid characterId, int amount, CancellationToken cancellationToken)
    {
        const string ensureSql = """
            INSERT INTO game.wealth (player_id, game_state_id, copper, silver, gold, platinum)
            VALUES (@characterId, @gameStateId, 0, 0, 0, 0)
            ON CONFLICT (player_id) DO NOTHING;
        """;
        await using (var ensure = new NpgsqlCommand(ensureSql, connection, transaction))
        {
            ensure.Parameters.AddWithValue("gameStateId", gameStateId);
            ensure.Parameters.AddWithValue("characterId", characterId);
            await ensure.ExecuteNonQueryAsync(cancellationToken);
        }

        const string sql = """
            UPDATE game.wealth
            SET gold = gold + @amount
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("amount", Math.Max(0, amount));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertLogAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, string text, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.game_log_entries (game_state_id, turn_number, type, text, important)
            SELECT @gameStateId, turn_number, 'combat', @text, true
            FROM game.game_states
            WHERE id = @gameStateId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("text", text);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IReadOnlyList<string> ParseConditions(string json)
        => JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

    private sealed record ParticipantSnapshot(
        Guid Id,
        string ActorType,
        Guid ActorId,
        int HpCurrent,
        IReadOnlyList<string> Conditions,
        bool IsEnemy,
        int XpReward,
        int CurrencyReward)
    {
        public bool Defeated => HpCurrent <= 0
            || Conditions.Any(condition =>
                string.Equals(condition, "повержен", StringComparison.OrdinalIgnoreCase)
                || string.Equals(condition, "defeated", StringComparison.OrdinalIgnoreCase)
                || string.Equals(condition, "без сознания", StringComparison.OrdinalIgnoreCase));
    }

    private sealed record CombatSnapshot(
        Guid CombatId,
        IReadOnlyList<ParticipantSnapshot> Participants,
        IReadOnlyList<Guid> DefeatedMonsterIds,
        IReadOnlyList<Guid> DefeatedCharacterIds,
        IReadOnlyList<Guid> CharacterIds,
        int EnemyCount,
        int DefeatedEnemyCount,
        int CharacterCount,
        int DefeatedCharacterCount,
        int XpRewardTotal,
        int CurrencyRewardTotal)
    {
        public static CombatSnapshot From(Guid combatId, IReadOnlyList<ParticipantSnapshot> participants)
        {
            var enemies = participants.Where(p => p.IsEnemy || string.Equals(p.ActorType, "monster", StringComparison.OrdinalIgnoreCase)).ToArray();
            var characters = participants.Where(p => string.Equals(p.ActorType, "character", StringComparison.OrdinalIgnoreCase) && !p.IsEnemy).ToArray();
            var defeatedEnemies = enemies.Where(p => p.Defeated).ToArray();
            var defeatedCharacters = characters.Where(p => p.Defeated).ToArray();

            return new CombatSnapshot(
                combatId,
                participants,
                defeatedEnemies.Where(p => string.Equals(p.ActorType, "monster", StringComparison.OrdinalIgnoreCase)).Select(p => p.ActorId).Distinct().ToArray(),
                defeatedCharacters.Select(p => p.ActorId).Distinct().ToArray(),
                characters.Select(p => p.ActorId).Distinct().ToArray(),
                enemies.Length,
                defeatedEnemies.Length,
                characters.Length,
                defeatedCharacters.Length,
                defeatedEnemies.Sum(p => Math.Max(0, p.XpReward)),
                defeatedEnemies.Sum(p => Math.Max(0, p.CurrencyReward)));
        }
    }
}
