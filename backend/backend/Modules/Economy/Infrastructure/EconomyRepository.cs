using System.Text.Json;
using backend.Infrastructure.Database;
using backend.Modules.Characters.Infrastructure;
using backend.Modules.Economy.Contracts;
using backend.Shared.Kernel;
using Npgsql;
using NpgsqlTypes;

namespace backend.Modules.Economy.Infrastructure;

public sealed class EconomyRepository : IEconomyRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public EconomyRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<JsonElement>?> GetLootAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await GameStateExistsAsync(connection, accountId, gameStateId, cancellationToken))
        {
            return null;
        }

        const string sql = """
            SELECT jsonb_build_object(
                'id', lc.id,
                'gameStateId', lc.game_state_id,
                'sourceType', lc.source_type,
                'sourceId', lc.source_id,
                'name', lc.name,
                'status', lc.status,
                'currencyAmount', lc.currency_amount,
                'metadata', lc.metadata,
                'createdAt', lc.created_at,
                'claimedAt', lc.claimed_at,
                'claimedByCharacterId', lc.claimed_by_character_id,
                'items', COALESCE((
                    SELECT jsonb_agg(jsonb_build_object(
                        'id', li.id,
                        'name', li.name,
                        'description', li.description,
                        'quantity', li.quantity,
                        'itemType', li.item_type,
                        'rarity', li.rarity,
                        'metadata', li.metadata
                    ) ORDER BY li.name, li.id)
                    FROM game.loot_items li
                    WHERE li.loot_container_id = lc.id
                      AND li.game_state_id = lc.game_state_id
                ), '[]'::jsonb)
            )::text
            FROM game.loot_containers lc
            WHERE lc.game_state_id = @gameStateId
            ORDER BY lc.created_at DESC, lc.id DESC;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<JsonElement>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(RpgDbJson.ParseElement(reader.GetString(0)));
        }

        return result;
    }

    public async Task<JsonElement?> GetLootContainerAsync(Guid accountId, Guid gameStateId, Guid lootContainerId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await SelectLootContainerAsync(connection, null, accountId, gameStateId, lootContainerId, cancellationToken);
    }

    public async Task<JsonElement?> CreateLootAsync(Guid accountId, Guid gameStateId, CreateLootContainerRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!await GameStateExistsAsync(connection, transaction, accountId, gameStateId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            foreach (var item in request.ResolvedItems)
            {
                if (string.IsNullOrWhiteSpace(item.ResolvedName))
                {
                    throw new RpgValidationException("Название предмета добычи обязательно.");
                }
            }

            const string insertContainerSql = """
                INSERT INTO game.loot_containers
                (
                    game_state_id,
                    source_type,
                    source_id,
                    name,
                    currency_amount,
                    metadata
                )
                VALUES
                (
                    @gameStateId,
                    @sourceType,
                    @sourceId,
                    @name,
                    @currencyAmount,
                    @metadata::jsonb
                )
                RETURNING id;
            """;

            Guid lootContainerId;
            await using (var command = new NpgsqlCommand(insertContainerSql, connection, transaction))
            {
                command.Parameters.AddWithValue("gameStateId", gameStateId);
                command.Parameters.AddWithValue("sourceType", DbString(request.ResolvedSourceType));
                command.Parameters.AddWithValue("sourceId", request.ResolvedSourceId.HasValue ? request.ResolvedSourceId.Value : DBNull.Value);
                command.Parameters.AddWithValue("name", request.ResolvedName);
                command.Parameters.AddWithValue("currencyAmount", request.ResolvedCurrencyAmount);
                AddJsonb(command, "metadata", request.ResolvedMetadata?.GetRawText() ?? "{}");
                lootContainerId = (Guid)(await command.ExecuteScalarAsync(cancellationToken)
                    ?? throw new InvalidOperationException("Loot container id was not returned."));
            }

            foreach (var item in request.ResolvedItems)
            {
                await InsertLootItemAsync(connection, transaction, gameStateId, lootContainerId, item, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return await GetLootContainerAsync(accountId, gameStateId, lootContainerId, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<JsonElement?> ClaimLootAsync(Guid accountId, Guid gameStateId, Guid lootContainerId, Guid characterId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!await CharacterExistsAsync(connection, transaction, accountId, gameStateId, characterId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var container = await GetLootContainerForUpdateAsync(connection, transaction, accountId, gameStateId, lootContainerId, cancellationToken);
            if (container is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            if (!string.Equals(container.Value.Status, "available", StringComparison.OrdinalIgnoreCase))
            {
                throw new RpgConflictException("Эта добыча уже получена или недоступна.");
            }

            var claimedItemIds = new List<Guid>();
            foreach (var item in await GetLootItemsAsync(connection, transaction, gameStateId, lootContainerId, cancellationToken))
            {
                claimedItemIds.Add(await InsertInventoryItemAsync(
                    connection,
                    transaction,
                    gameStateId,
                    characterId,
                    item.Name,
                    item.Description,
                    item.Quantity,
                    item.ItemType,
                    item.Rarity,
                    cancellationToken));
            }

            if (container.Value.CurrencyAmount > 0)
            {
                await AddCurrencyInternalAsync(connection, transaction, gameStateId, characterId, container.Value.CurrencyAmount, cancellationToken);
            }

            const string updateSql = """
                UPDATE game.loot_containers
                SET status = 'claimed',
                    claimed_at = now(),
                    claimed_by_character_id = @characterId
                WHERE game_state_id = @gameStateId
                  AND id = @lootContainerId;
            """;

            await using (var update = new NpgsqlCommand(updateSql, connection, transaction))
            {
                update.Parameters.AddWithValue("gameStateId", gameStateId);
                update.Parameters.AddWithValue("lootContainerId", lootContainerId);
                update.Parameters.AddWithValue("characterId", characterId);
                await update.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            var claimed = await GetLootContainerAsync(accountId, gameStateId, lootContainerId, cancellationToken);
            return JsonSerializer.SerializeToElement(new
            {
                loot = claimed,
                claimedItemIds,
                currencyAdded = container.Value.CurrencyAmount,
                characterId
            });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<JsonElement?> GetCurrencyAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, null, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        await EnsureWealthAsync(connection, null, gameStateId, characterId, cancellationToken);
        return await SelectCurrencyAsync(connection, null, gameStateId, characterId, cancellationToken);
    }

    public async Task<JsonElement?> AddCurrencyAsync(Guid accountId, Guid gameStateId, Guid characterId, int amount, string reason, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, null, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        await AddCurrencyInternalAsync(connection, null, gameStateId, characterId, amount, cancellationToken);
        return await SelectCurrencyWithReasonAsync(connection, null, gameStateId, characterId, amount, reason, "add_currency", cancellationToken);
    }

    public async Task<JsonElement?> SpendCurrencyAsync(Guid accountId, Guid gameStateId, Guid characterId, int amount, string reason, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, null, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        await EnsureWealthAsync(connection, null, gameStateId, characterId, cancellationToken);
        const string sql = """
            UPDATE game.wealth
            SET gold = gold - @amount
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId
              AND gold >= @amount;
        """;

        await using (var command = new NpgsqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("gameStateId", gameStateId);
            command.Parameters.AddWithValue("characterId", characterId);
            command.Parameters.AddWithValue("amount", amount);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                throw new RpgConflictException("Недостаточно золота.");
            }
        }

        return await SelectCurrencyWithReasonAsync(connection, null, gameStateId, characterId, amount, reason, "spend_currency", cancellationToken);
    }

    public async Task<JsonElement?> CompleteQuestAsync(Guid accountId, Guid gameStateId, Guid questId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE game.quests q
            SET status = 'completed'
            FROM game.game_states gs
            WHERE gs.id = q.game_state_id
              AND gs.account_id = @accountId
              AND q.game_state_id = @gameStateId
              AND q.id = @questId
            RETURNING jsonb_build_object(
                'id', q.id,
                'gameStateId', q.game_state_id,
                'title', q.title,
                'status', q.status,
                'rewardExperience', q.reward_experience,
                'rewardGold', q.reward_gold
            )::text;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("questId", questId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    public async Task<JsonElement?> GrantQuestRewardAsync(Guid accountId, Guid gameStateId, Guid questId, Guid characterId, int xpAmount, int currencyAmount, IReadOnlyList<CreateLootItemRequest> items, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!await CharacterExistsAsync(connection, transaction, accountId, gameStateId, characterId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var quest = await GetQuestRewardDefaultsAsync(connection, transaction, accountId, gameStateId, questId, cancellationToken);
            if (quest is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            if (await QuestRewardAlreadyGrantedAsync(connection, transaction, gameStateId, questId, cancellationToken))
            {
                throw new RpgConflictException("Награда за этот квест уже выдана.");
            }

            var finalXp = xpAmount > 0 ? xpAmount : quest.Value.Xp;
            var finalCurrency = currencyAmount > 0 ? currencyAmount : quest.Value.Gold;
            var grantedItemIds = new List<Guid>();

            if (finalXp > 0)
            {
                await AddExperienceInternalAsync(connection, transaction, gameStateId, characterId, finalXp, cancellationToken);
            }

            if (finalCurrency > 0)
            {
                await AddCurrencyInternalAsync(connection, transaction, gameStateId, characterId, finalCurrency, cancellationToken);
            }

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.ResolvedName))
                {
                    throw new RpgValidationException("Название предмета награды обязательно.");
                }

                grantedItemIds.Add(await InsertInventoryItemAsync(
                    connection,
                    transaction,
                    gameStateId,
                    characterId,
                    item.ResolvedName,
                    item.ResolvedDescription,
                    item.ResolvedQuantity,
                    item.ResolvedItemType,
                    item.ResolvedRarity,
                    cancellationToken));
            }

            const string insertRewardSql = """
                INSERT INTO game.quest_rewards
                (
                    game_state_id,
                    quest_id,
                    xp_amount,
                    currency_amount,
                    items,
                    granted_at,
                    granted_to_character_id
                )
                VALUES
                (
                    @gameStateId,
                    @questId,
                    @xp,
                    @currency,
                    @items::jsonb,
                    now(),
                    @characterId
                )
                RETURNING id;
            """;

            Guid rewardId;
            await using (var command = new NpgsqlCommand(insertRewardSql, connection, transaction))
            {
                command.Parameters.AddWithValue("gameStateId", gameStateId);
                command.Parameters.AddWithValue("questId", questId);
                command.Parameters.AddWithValue("xp", finalXp);
                command.Parameters.AddWithValue("currency", finalCurrency);
                AddJsonb(command, "items", JsonSerializer.Serialize(items));
                command.Parameters.AddWithValue("characterId", characterId);
                rewardId = (Guid)(await command.ExecuteScalarAsync(cancellationToken)
                    ?? throw new InvalidOperationException("Quest reward id was not returned."));
            }

            await transaction.CommitAsync(cancellationToken);
            return JsonSerializer.SerializeToElement(new
            {
                rewardId,
                questId,
                characterId,
                xpAdded = finalXp,
                currencyAdded = finalCurrency,
                grantedItemIds
            });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task InsertLootItemAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid lootContainerId, CreateLootItemRequest item, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.loot_items
            (
                game_state_id,
                loot_container_id,
                name,
                description,
                quantity,
                item_type,
                rarity,
                metadata
            )
            VALUES
            (
                @gameStateId,
                @lootContainerId,
                @name,
                @description,
                @quantity,
                @itemType,
                @rarity,
                @metadata::jsonb
            );
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("lootContainerId", lootContainerId);
        command.Parameters.AddWithValue("name", item.ResolvedName);
        command.Parameters.AddWithValue("description", DbString(item.ResolvedDescription));
        command.Parameters.AddWithValue("quantity", item.ResolvedQuantity);
        command.Parameters.AddWithValue("itemType", DbString(item.ResolvedItemType));
        command.Parameters.AddWithValue("rarity", DbString(item.ResolvedRarity));
        AddJsonb(command, "metadata", item.ResolvedMetadata?.GetRawText() ?? "{}");
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Guid> InsertInventoryItemAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid gameStateId,
        Guid characterId,
        string name,
        string? description,
        int quantity,
        string? itemType,
        string? rarity,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.item_instances
            (
                game_state_id,
                name,
                item_type,
                description,
                quantity,
                rarity,
                owner_kind,
                owner_id,
                tags
            )
            VALUES
            (
                @gameStateId,
                @name,
                @itemType,
                @description,
                @quantity,
                @rarity,
                'player_inventory',
                @characterId,
                '[]'::jsonb
            )
            RETURNING id;
        """;

        await using var command = transaction is null
            ? new NpgsqlCommand(sql, connection)
            : new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("name", name.Trim());
        command.Parameters.AddWithValue("description", DbString(description));
        command.Parameters.AddWithValue("quantity", Math.Max(1, quantity));
        command.Parameters.AddWithValue("itemType", string.IsNullOrWhiteSpace(itemType) ? "misc" : itemType.Trim());
        command.Parameters.AddWithValue("rarity", string.IsNullOrWhiteSpace(rarity) ? "common" : rarity.Trim());
        return (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Inventory item id was not returned."));
    }

    private static async Task AddCurrencyInternalAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid gameStateId, Guid characterId, int amount, CancellationToken cancellationToken)
    {
        await EnsureWealthAsync(connection, transaction, gameStateId, characterId, cancellationToken);
        const string sql = """
            UPDATE game.wealth
            SET gold = gold + @amount
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId;
        """;

        await using var command = transaction is null
            ? new NpgsqlCommand(sql, connection)
            : new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("amount", Math.Max(0, amount));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task AddExperienceInternalAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid characterId, int amount, CancellationToken cancellationToken)
    {
        if (amount > 0)
        {
            await CharacterProgressionSql.AddExperienceAsync(connection, transaction, gameStateId, characterId, amount, cancellationToken);
        }
    }

    private static async Task EnsureWealthAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.wealth (player_id, game_state_id, copper, silver, gold, platinum)
            VALUES (@characterId, @gameStateId, 0, 0, 0, 0)
            ON CONFLICT (player_id) DO NOTHING;
        """;

        await using var command = transaction is null
            ? new NpgsqlCommand(sql, connection)
            : new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<JsonElement?> SelectCurrencyAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT jsonb_build_object(
                'characterId', w.player_id,
                'gameStateId', w.game_state_id,
                'copper', w.copper,
                'silver', w.silver,
                'gold', w.gold,
                'platinum', w.platinum
            )::text
            FROM game.wealth w
            WHERE w.game_state_id = @gameStateId
              AND w.player_id = @characterId
            LIMIT 1;
        """;

        await using var command = transaction is null
            ? new NpgsqlCommand(sql, connection)
            : new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    private static async Task<JsonElement?> SelectCurrencyWithReasonAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid gameStateId, Guid characterId, int amount, string reason, string operation, CancellationToken cancellationToken)
    {
        var balance = await SelectCurrencyAsync(connection, transaction, gameStateId, characterId, cancellationToken);
        return balance.HasValue
            ? JsonSerializer.SerializeToElement(new { operation, amount, reason, balance = balance.Value })
            : null;
    }

    private static async Task<JsonElement?> SelectLootContainerAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid accountId, Guid gameStateId, Guid lootContainerId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT jsonb_build_object(
                'id', lc.id,
                'gameStateId', lc.game_state_id,
                'sourceType', lc.source_type,
                'sourceId', lc.source_id,
                'name', lc.name,
                'status', lc.status,
                'currencyAmount', lc.currency_amount,
                'metadata', lc.metadata,
                'createdAt', lc.created_at,
                'claimedAt', lc.claimed_at,
                'claimedByCharacterId', lc.claimed_by_character_id,
                'items', COALESCE((
                    SELECT jsonb_agg(jsonb_build_object(
                        'id', li.id,
                        'name', li.name,
                        'description', li.description,
                        'quantity', li.quantity,
                        'itemType', li.item_type,
                        'rarity', li.rarity,
                        'metadata', li.metadata
                    ) ORDER BY li.name, li.id)
                    FROM game.loot_items li
                    WHERE li.loot_container_id = lc.id
                      AND li.game_state_id = lc.game_state_id
                ), '[]'::jsonb)
            )::text
            FROM game.loot_containers lc
            JOIN game.game_states gs ON gs.id = lc.game_state_id
            WHERE gs.account_id = @accountId
              AND lc.game_state_id = @gameStateId
              AND lc.id = @lootContainerId
            LIMIT 1;
        """;

        await using var command = transaction is null
            ? new NpgsqlCommand(sql, connection)
            : new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("lootContainerId", lootContainerId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    private static async Task<LootContainerRow?> GetLootContainerForUpdateAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid accountId, Guid gameStateId, Guid lootContainerId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT lc.id, lc.status, lc.currency_amount
            FROM game.loot_containers lc
            JOIN game.game_states gs ON gs.id = lc.game_state_id
            WHERE gs.account_id = @accountId
              AND lc.game_state_id = @gameStateId
              AND lc.id = @lootContainerId
            FOR UPDATE OF lc;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("lootContainerId", lootContainerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new LootContainerRow(reader.GetGuid(0), reader.GetString(1), reader.GetInt32(2))
            : null;
    }

    private static async Task<IReadOnlyList<LootItemRow>> GetLootItemsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid lootContainerId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT name, description, quantity, item_type, rarity
            FROM game.loot_items
            WHERE game_state_id = @gameStateId
              AND loot_container_id = @lootContainerId
            ORDER BY name, id;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("lootContainerId", lootContainerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<LootItemRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new LootItemRow(
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.GetInt32(2),
                reader.IsDBNull(3) ? "misc" : reader.GetString(3),
                reader.IsDBNull(4) ? "common" : reader.GetString(4)));
        }

        return result;
    }

    private static async Task<QuestRewardDefaults?> GetQuestRewardDefaultsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid accountId, Guid gameStateId, Guid questId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT q.reward_experience, q.reward_gold
            FROM game.quests q
            JOIN game.game_states gs ON gs.id = q.game_state_id
            WHERE gs.account_id = @accountId
              AND q.game_state_id = @gameStateId
              AND q.id = @questId
            FOR UPDATE OF q;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("questId", questId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new QuestRewardDefaults(reader.GetInt32(0), reader.GetInt32(1))
            : null;
    }

    private static async Task<bool> QuestRewardAlreadyGrantedAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid questId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM game.quest_rewards WHERE game_state_id = @gameStateId AND quest_id = @questId AND granted_at IS NOT NULL);";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("questId", questId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task<bool> GameStateExistsAsync(NpgsqlConnection connection, Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        => await GameStateExistsAsync(connection, null, accountId, gameStateId, cancellationToken);

    private static async Task<bool> GameStateExistsAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM game.game_states WHERE id = @gameStateId AND account_id = @accountId);";
        await using var command = transaction is null
            ? new NpgsqlCommand(sql, connection)
            : new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task<bool> CharacterExistsAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM game.players p
                JOIN game.game_states gs ON gs.id = p.game_state_id
                WHERE gs.account_id = @accountId
                  AND p.game_state_id = @gameStateId
                  AND p.id = @characterId
            );
        """;

        await using var command = transaction is null
            ? new NpgsqlCommand(sql, connection)
            : new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static object DbString(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static void AddJsonb(NpgsqlCommand command, string name, string json)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Jsonb);
        parameter.Value = string.IsNullOrWhiteSpace(json) ? "{}" : json;
    }

    private readonly record struct LootContainerRow(Guid Id, string Status, int CurrencyAmount);

    private readonly record struct LootItemRow(string Name, string? Description, int Quantity, string ItemType, string Rarity);

    private readonly record struct QuestRewardDefaults(int Xp, int Gold);
}
