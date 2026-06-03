using System.Text.Json;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.World.Contracts;
using backend.Infrastructure.Database;
using Npgsql;
using NpgsqlTypes;

namespace backend.Modules.World.Infrastructure;

public sealed class WorldRepository : IWorldRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public WorldRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<JsonElement>?> ListAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid? parentId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await GameStateExistsAsync(connection, accountId, gameStateId, cancellationToken))
        {
            return null;
        }

        if (!await ParentExistsAsync(connection, gameStateId, kind, parentId, cancellationToken))
        {
            return null;
        }

        var sql = GetListSql(kind);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("parentId", parentId.HasValue ? parentId.Value : DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<JsonElement>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(RpgDbJson.ParseElement(reader.GetString(0)));
        }

        return result;
    }

    public async Task<JsonElement?> GetAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid entityId, Guid? parentId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await GameStateExistsAsync(connection, accountId, gameStateId, cancellationToken))
        {
            return null;
        }

        if (!await ParentExistsAsync(connection, gameStateId, kind, parentId, cancellationToken))
        {
            return null;
        }

        var sql = GetDetailSql(kind);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("entityId", entityId);
        command.Parameters.AddWithValue("parentId", parentId.HasValue ? parentId.Value : DBNull.Value);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    public async Task<Guid?> CreateAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid? parentId, JsonElement payload, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await GameStateExistsAsync(connection, accountId, gameStateId, cancellationToken))
        {
            return null;
        }

        if (!await ParentExistsAsync(connection, gameStateId, kind, parentId, cancellationToken))
        {
            return null;
        }

        await ValidateReferencesAsync(connection, gameStateId, kind, parentId, payload, cancellationToken);

        var sql = GetCreateSql(kind);
        await using var command = new NpgsqlCommand(sql, connection);
        EnsureCreateRequired(kind, payload);
        AddCommonParameters(command, gameStateId, parentId, payload);
        AddKindParameters(command, kind, payload);

        return (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("World entity id was not returned."));
    }

    public async Task<bool?> UpdateAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid entityId, Guid? parentId, JsonElement payload, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await GameStateExistsAsync(connection, accountId, gameStateId, cancellationToken))
        {
            return null;
        }

        if (!await ParentExistsAsync(connection, gameStateId, kind, parentId, cancellationToken))
        {
            return null;
        }

        await ValidateReferencesAsync(connection, gameStateId, kind, parentId, payload, cancellationToken);

        var sql = GetUpdateSql(kind);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("entityId", entityId);
        AddCommonParameters(command, gameStateId, parentId, payload);
        AddKindParameters(command, kind, payload);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool?> DeleteAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid entityId, Guid? parentId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await GameStateExistsAsync(connection, accountId, gameStateId, cancellationToken))
        {
            return null;
        }

        if (!await ParentExistsAsync(connection, gameStateId, kind, parentId, cancellationToken))
        {
            return null;
        }

        var sql = GetDeleteSql(kind);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("entityId", entityId);
        command.Parameters.AddWithValue("parentId", parentId.HasValue ? parentId.Value : DBNull.Value);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static string GetListSql(WorldEntityKind kind) => kind switch
    {
        WorldEntityKind.Location => """
            SELECT jsonb_build_object('id', l.id, 'gameStateId', l.game_state_id, 'name', l.name, 'description', l.description, 'createdAt', l.created_at)::text
            FROM game.locations l
            WHERE l.game_state_id = @gameStateId
            ORDER BY l.name, l.id;
        """,
        WorldEntityKind.LocationExit => """
            SELECT jsonb_build_object('id', e.id, 'gameStateId', e.game_state_id, 'locationId', e.location_id, 'direction', e.direction, 'targetLocationId', e.target_location_id, 'description', e.description, 'isLocked', e.is_locked)::text
            FROM game.location_exits e
            WHERE e.game_state_id = @gameStateId AND e.location_id = @parentId
            ORDER BY e.direction, e.id;
        """,
        WorldEntityKind.WorldObject => """
            SELECT jsonb_build_object('id', o.id, 'gameStateId', o.game_state_id, 'locationId', o.location_id, 'name', o.name, 'objectType', o.object_type, 'description', o.description, 'state', o.state, 'tags', o.tags)::text
            FROM game.world_objects o
            WHERE o.game_state_id = @gameStateId
            ORDER BY o.name, o.id;
        """,
        WorldEntityKind.Container => """
            SELECT jsonb_build_object('id', c.id, 'gameStateId', c.game_state_id, 'locationId', c.location_id, 'name', c.name, 'description', c.description, 'isLocked', c.is_locked, 'keyItemId', c.key_item_id)::text
            FROM game.world_containers c
            WHERE c.game_state_id = @gameStateId
            ORDER BY c.name, c.id;
        """,
        WorldEntityKind.Npc => """
            SELECT jsonb_build_object('id', n.id, 'gameStateId', n.game_state_id, 'locationId', n.location_id, 'name', n.name, 'role', n.role, 'attitude', n.attitude, 'description', n.description, 'isAlive', n.is_alive)::text
            FROM game.npcs n
            WHERE n.game_state_id = @gameStateId
            ORDER BY n.name, n.id;
        """,
        WorldEntityKind.Faction => """
            SELECT jsonb_build_object('id', f.id, 'gameStateId', f.game_state_id, 'name', f.name, 'reputation', f.reputation, 'description', f.description)::text
            FROM game.factions f
            WHERE f.game_state_id = @gameStateId
            ORDER BY f.name, f.id;
        """,
        WorldEntityKind.Quest => """
            SELECT jsonb_build_object('id', q.id, 'gameStateId', q.game_state_id, 'title', q.title, 'description', q.description, 'status', q.status, 'rewardExperience', q.reward_experience, 'rewardCopper', q.reward_copper, 'rewardSilver', q.reward_silver, 'rewardGold', q.reward_gold, 'rewardPlatinum', q.reward_platinum)::text
            FROM game.quests q
            WHERE q.game_state_id = @gameStateId
            ORDER BY q.status, q.title, q.id;
        """,
        WorldEntityKind.QuestStep => """
            SELECT jsonb_build_object('id', s.id, 'gameStateId', s.game_state_id, 'questId', s.quest_id, 'description', s.description, 'isCompleted', s.is_completed, 'sortOrder', s.sort_order)::text
            FROM game.quest_steps s
            WHERE s.game_state_id = @gameStateId AND s.quest_id = @parentId
            ORDER BY s.sort_order, s.id;
        """,
        WorldEntityKind.Monster => """
            SELECT jsonb_build_object('id', m.id, 'gameStateId', m.game_state_id, 'locationId', m.location_id, 'name', m.name, 'monsterType', m.monster_type, 'description', m.description, 'hpCurrent', m.hp_current, 'hpMax', m.hp_max, 'armorClass', m.armor_class, 'initiativeBonus', m.initiative_bonus, 'isAlive', m.is_alive, 'status', COALESCE(m.status, CASE WHEN m.is_alive THEN 'alive' ELSE 'dead' END), 'xpReward', COALESCE(m.xp_reward, 0), 'currencyReward', COALESCE(m.currency_reward, 0), 'stats', m.stats, 'abilities', m.abilities, 'loot', m.loot, 'tags', m.tags, 'createdAt', m.created_at, 'updatedAt', m.updated_at)::text
            FROM game.monsters m
            WHERE m.game_state_id = @gameStateId
            ORDER BY m.name, m.id;
        """,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private static string GetDetailSql(WorldEntityKind kind) => kind switch
    {
        WorldEntityKind.Location => "SELECT jsonb_build_object('id', l.id, 'gameStateId', l.game_state_id, 'name', l.name, 'description', l.description, 'createdAt', l.created_at)::text FROM game.locations l WHERE l.game_state_id = @gameStateId AND l.id = @entityId LIMIT 1;",
        WorldEntityKind.LocationExit => "SELECT jsonb_build_object('id', e.id, 'gameStateId', e.game_state_id, 'locationId', e.location_id, 'direction', e.direction, 'targetLocationId', e.target_location_id, 'description', e.description, 'isLocked', e.is_locked)::text FROM game.location_exits e WHERE e.game_state_id = @gameStateId AND e.location_id = @parentId AND e.id = @entityId LIMIT 1;",
        WorldEntityKind.WorldObject => "SELECT jsonb_build_object('id', o.id, 'gameStateId', o.game_state_id, 'locationId', o.location_id, 'name', o.name, 'objectType', o.object_type, 'description', o.description, 'state', o.state, 'tags', o.tags)::text FROM game.world_objects o WHERE o.game_state_id = @gameStateId AND o.id = @entityId LIMIT 1;",
        WorldEntityKind.Container => "SELECT jsonb_build_object('id', c.id, 'gameStateId', c.game_state_id, 'locationId', c.location_id, 'name', c.name, 'description', c.description, 'isLocked', c.is_locked, 'keyItemId', c.key_item_id)::text FROM game.world_containers c WHERE c.game_state_id = @gameStateId AND c.id = @entityId LIMIT 1;",
        WorldEntityKind.Npc => "SELECT jsonb_build_object('id', n.id, 'gameStateId', n.game_state_id, 'locationId', n.location_id, 'name', n.name, 'role', n.role, 'attitude', n.attitude, 'description', n.description, 'isAlive', n.is_alive)::text FROM game.npcs n WHERE n.game_state_id = @gameStateId AND n.id = @entityId LIMIT 1;",
        WorldEntityKind.Faction => "SELECT jsonb_build_object('id', f.id, 'gameStateId', f.game_state_id, 'name', f.name, 'reputation', f.reputation, 'description', f.description)::text FROM game.factions f WHERE f.game_state_id = @gameStateId AND f.id = @entityId LIMIT 1;",
        WorldEntityKind.Quest => "SELECT jsonb_build_object('id', q.id, 'gameStateId', q.game_state_id, 'title', q.title, 'description', q.description, 'status', q.status, 'rewardExperience', q.reward_experience, 'rewardCopper', q.reward_copper, 'rewardSilver', q.reward_silver, 'rewardGold', q.reward_gold, 'rewardPlatinum', q.reward_platinum)::text FROM game.quests q WHERE q.game_state_id = @gameStateId AND q.id = @entityId LIMIT 1;",
        WorldEntityKind.QuestStep => "SELECT jsonb_build_object('id', s.id, 'gameStateId', s.game_state_id, 'questId', s.quest_id, 'description', s.description, 'isCompleted', s.is_completed, 'sortOrder', s.sort_order)::text FROM game.quest_steps s WHERE s.game_state_id = @gameStateId AND s.quest_id = @parentId AND s.id = @entityId LIMIT 1;",
        WorldEntityKind.Monster => "SELECT jsonb_build_object('id', m.id, 'gameStateId', m.game_state_id, 'locationId', m.location_id, 'name', m.name, 'monsterType', m.monster_type, 'description', m.description, 'hpCurrent', m.hp_current, 'hpMax', m.hp_max, 'armorClass', m.armor_class, 'initiativeBonus', m.initiative_bonus, 'isAlive', m.is_alive, 'status', COALESCE(m.status, CASE WHEN m.is_alive THEN 'alive' ELSE 'dead' END), 'xpReward', COALESCE(m.xp_reward, 0), 'currencyReward', COALESCE(m.currency_reward, 0), 'stats', m.stats, 'abilities', m.abilities, 'loot', m.loot, 'tags', m.tags, 'createdAt', m.created_at, 'updatedAt', m.updated_at)::text FROM game.monsters m WHERE m.game_state_id = @gameStateId AND m.id = @entityId LIMIT 1;",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private static string GetCreateSql(WorldEntityKind kind) => kind switch
    {
        WorldEntityKind.Location => """
            INSERT INTO game.locations (game_state_id, name, description)
            VALUES (@gameStateId, @name, @description)
            RETURNING id;
        """,
        WorldEntityKind.LocationExit => """
            INSERT INTO game.location_exits (game_state_id, location_id, direction, target_location_id, description, is_locked)
            VALUES (@gameStateId, @parentId, @direction, @targetLocationId, @description, COALESCE(@isLocked, false))
            RETURNING id;
        """,
        WorldEntityKind.WorldObject => """
            INSERT INTO game.world_objects (game_state_id, location_id, name, object_type, description, state, tags)
            VALUES (@gameStateId, @locationId, @name, @objectType, @description, COALESCE(@state, 'обычное'), COALESCE(@tags, '[]'::jsonb))
            RETURNING id;
        """,
        WorldEntityKind.Container => """
            INSERT INTO game.world_containers (game_state_id, location_id, name, description, is_locked, key_item_id)
            VALUES (@gameStateId, @locationId, @name, @description, COALESCE(@isLocked, false), @keyItemId)
            RETURNING id;
        """,
        WorldEntityKind.Npc => """
            INSERT INTO game.npcs (game_state_id, location_id, name, role, attitude, description, is_alive)
            VALUES (@gameStateId, @locationId, @name, @role, COALESCE(@attitude, 'neutral'), @description, COALESCE(@isAlive, true))
            RETURNING id;
        """,
        WorldEntityKind.Faction => """
            INSERT INTO game.factions (game_state_id, name, reputation, description)
            VALUES (@gameStateId, @name, COALESCE(@reputation, 0), @description)
            RETURNING id;
        """,
        WorldEntityKind.Quest => """
            INSERT INTO game.quests (game_state_id, title, description, status, reward_experience, reward_copper, reward_silver, reward_gold, reward_platinum)
            VALUES (@gameStateId, @title, @description, COALESCE(@status, 'active'), COALESCE(@rewardExperience, 0), COALESCE(@rewardCopper, 0), COALESCE(@rewardSilver, 0), COALESCE(@rewardGold, 0), COALESCE(@rewardPlatinum, 0))
            RETURNING id;
        """,
        WorldEntityKind.QuestStep => """
            INSERT INTO game.quest_steps (game_state_id, quest_id, description, is_completed, sort_order)
            VALUES (@gameStateId, @parentId, @description, COALESCE(@isCompleted, false), COALESCE(@sortOrder, 0))
            RETURNING id;
        """,
        WorldEntityKind.Monster => """
            INSERT INTO game.monsters (game_state_id, location_id, name, monster_type, description, hp_current, hp_max, armor_class, initiative_bonus, is_alive, status, xp_reward, currency_reward, stats, abilities, loot, tags)
            VALUES (@gameStateId, @locationId, @name, @monsterType, @description, COALESCE(@hpCurrent, 1), COALESCE(@hpMax, 1), COALESCE(@armorClass, 10), COALESCE(@initiativeBonus, 0), COALESCE(@isAlive, true), COALESCE(@status, CASE WHEN COALESCE(@isAlive, true) THEN 'alive' ELSE 'dead' END), COALESCE(@xpReward, 0), COALESCE(@currencyReward, 0), COALESCE(@stats, '{}'::jsonb), COALESCE(@abilities, '[]'::jsonb), COALESCE(@loot, '[]'::jsonb), COALESCE(@tags, '[]'::jsonb))
            RETURNING id;
        """,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private static string GetUpdateSql(WorldEntityKind kind) => kind switch
    {
        WorldEntityKind.Location => """
            UPDATE game.locations
            SET name = COALESCE(@name, name),
                description = COALESCE(@description, description)
            WHERE game_state_id = @gameStateId AND id = @entityId;
        """,
        WorldEntityKind.LocationExit => """
            UPDATE game.location_exits
            SET direction = COALESCE(@direction, direction),
                target_location_id = COALESCE(@targetLocationId, target_location_id),
                description = COALESCE(@description, description),
                is_locked = COALESCE(@isLocked, is_locked)
            WHERE game_state_id = @gameStateId AND location_id = @parentId AND id = @entityId;
        """,
        WorldEntityKind.WorldObject => """
            UPDATE game.world_objects
            SET location_id = COALESCE(@locationId, location_id),
                name = COALESCE(@name, name),
                object_type = COALESCE(@objectType, object_type),
                description = COALESCE(@description, description),
                state = COALESCE(@state, state),
                tags = COALESCE(@tags, tags)
            WHERE game_state_id = @gameStateId AND id = @entityId;
        """,
        WorldEntityKind.Container => """
            UPDATE game.world_containers
            SET location_id = COALESCE(@locationId, location_id),
                name = COALESCE(@name, name),
                description = COALESCE(@description, description),
                is_locked = COALESCE(@isLocked, is_locked),
                key_item_id = COALESCE(@keyItemId, key_item_id)
            WHERE game_state_id = @gameStateId AND id = @entityId;
        """,
        WorldEntityKind.Npc => """
            UPDATE game.npcs
            SET location_id = COALESCE(@locationId, location_id),
                name = COALESCE(@name, name),
                role = COALESCE(@role, role),
                attitude = COALESCE(@attitude, attitude),
                description = COALESCE(@description, description),
                is_alive = COALESCE(@isAlive, is_alive)
            WHERE game_state_id = @gameStateId AND id = @entityId;
        """,
        WorldEntityKind.Faction => """
            UPDATE game.factions
            SET name = COALESCE(@name, name),
                reputation = COALESCE(@reputation, reputation),
                description = COALESCE(@description, description)
            WHERE game_state_id = @gameStateId AND id = @entityId;
        """,
        WorldEntityKind.Quest => """
            UPDATE game.quests
            SET title = COALESCE(@title, title),
                description = COALESCE(@description, description),
                status = COALESCE(@status, status),
                reward_experience = COALESCE(@rewardExperience, reward_experience),
                reward_copper = COALESCE(@rewardCopper, reward_copper),
                reward_silver = COALESCE(@rewardSilver, reward_silver),
                reward_gold = COALESCE(@rewardGold, reward_gold),
                reward_platinum = COALESCE(@rewardPlatinum, reward_platinum)
            WHERE game_state_id = @gameStateId AND id = @entityId;
        """,
        WorldEntityKind.QuestStep => """
            UPDATE game.quest_steps
            SET description = COALESCE(@description, description),
                is_completed = COALESCE(@isCompleted, is_completed),
                sort_order = COALESCE(@sortOrder, sort_order)
            WHERE game_state_id = @gameStateId AND quest_id = @parentId AND id = @entityId;
        """,
        WorldEntityKind.Monster => """
            UPDATE game.monsters
            SET location_id = COALESCE(@locationId, location_id),
                name = COALESCE(@name, name),
                monster_type = COALESCE(@monsterType, monster_type),
                description = COALESCE(@description, description),
                hp_current = COALESCE(@hpCurrent, hp_current),
                hp_max = COALESCE(@hpMax, hp_max),
                armor_class = COALESCE(@armorClass, armor_class),
                initiative_bonus = COALESCE(@initiativeBonus, initiative_bonus),
                is_alive = COALESCE(@isAlive, is_alive),
                status = COALESCE(@status, status),
                xp_reward = COALESCE(@xpReward, xp_reward),
                currency_reward = COALESCE(@currencyReward, currency_reward),
                stats = COALESCE(@stats, stats),
                abilities = COALESCE(@abilities, abilities),
                loot = COALESCE(@loot, loot),
                tags = COALESCE(@tags, tags),
                updated_at = now()
            WHERE game_state_id = @gameStateId AND id = @entityId;
        """,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private static string GetDeleteSql(WorldEntityKind kind) => kind switch
    {
        WorldEntityKind.Location => "DELETE FROM game.locations WHERE game_state_id = @gameStateId AND id = @entityId;",
        WorldEntityKind.LocationExit => "DELETE FROM game.location_exits WHERE game_state_id = @gameStateId AND location_id = @parentId AND id = @entityId;",
        WorldEntityKind.WorldObject => "DELETE FROM game.world_objects WHERE game_state_id = @gameStateId AND id = @entityId;",
        WorldEntityKind.Container => "DELETE FROM game.world_containers WHERE game_state_id = @gameStateId AND id = @entityId;",
        WorldEntityKind.Npc => "DELETE FROM game.npcs WHERE game_state_id = @gameStateId AND id = @entityId;",
        WorldEntityKind.Faction => "DELETE FROM game.factions WHERE game_state_id = @gameStateId AND id = @entityId;",
        WorldEntityKind.Quest => "DELETE FROM game.quests WHERE game_state_id = @gameStateId AND id = @entityId;",
        WorldEntityKind.QuestStep => "DELETE FROM game.quest_steps WHERE game_state_id = @gameStateId AND quest_id = @parentId AND id = @entityId;",
        WorldEntityKind.Monster => "DELETE FROM game.monsters WHERE game_state_id = @gameStateId AND id = @entityId;",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private static async Task ValidateReferencesAsync(NpgsqlConnection connection, Guid gameStateId, WorldEntityKind kind, Guid? parentId, JsonElement payload, CancellationToken cancellationToken)
    {
        var locationId = kind switch
        {
            WorldEntityKind.LocationExit => GetOptionalGuid(payload, "targetLocationId", "target_location_id", "целеваяЛокацияId"),
            WorldEntityKind.WorldObject or WorldEntityKind.Container or WorldEntityKind.Npc or WorldEntityKind.Monster => GetOptionalGuid(payload, "locationId", "location_id", "локацияId"),
            _ => null
        };

        if (kind is WorldEntityKind.WorldObject or WorldEntityKind.Container && !locationId.HasValue)
        {
            throw new RpgValidationException("locationId is required.");
        }

        if (locationId.HasValue && !await ExistsAsync(connection, "game.locations", gameStateId, locationId.Value, cancellationToken))
        {
            throw new RpgValidationException("Referenced location does not belong to this GameState.");
        }

        var keyItemId = kind == WorldEntityKind.Container ? GetOptionalGuid(payload, "keyItemId", "key_item_id", "ключПредметId") : null;
        if (keyItemId.HasValue && !await ExistsAsync(connection, "game.item_instances", gameStateId, keyItemId.Value, cancellationToken))
        {
            throw new RpgValidationException("Referenced key item does not belong to this GameState.");
        }

        if (kind == WorldEntityKind.QuestStep && !parentId.HasValue)
        {
            throw new RpgValidationException("questId is required.");
        }
    }

    private static async Task<bool> ParentExistsAsync(NpgsqlConnection connection, Guid gameStateId, WorldEntityKind kind, Guid? parentId, CancellationToken cancellationToken)
    {
        return kind switch
        {
            WorldEntityKind.LocationExit => parentId.HasValue && await ExistsAsync(connection, "game.locations", gameStateId, parentId.Value, cancellationToken),
            WorldEntityKind.QuestStep => parentId.HasValue && await ExistsAsync(connection, "game.quests", gameStateId, parentId.Value, cancellationToken),
            _ => true
        };
    }

    private static void AddCommonParameters(NpgsqlCommand command, Guid gameStateId, Guid? parentId, JsonElement payload)
    {
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("parentId", parentId.HasValue ? parentId.Value : DBNull.Value);
        command.Parameters.AddWithValue("name", DbString(GetOptionalString(payload, "name", "название")));
        command.Parameters.AddWithValue("description", DbString(GetOptionalString(payload, "description", "описание")));
        AddNullableGuid(command, "locationId", GetOptionalGuid(payload, "locationId", "location_id", "локацияId"));
        AddJsonb(command, "tags", GetOptionalElement(payload, "tags", "теги")?.GetRawText());
    }

    private static void AddKindParameters(NpgsqlCommand command, WorldEntityKind kind, JsonElement payload)
    {
        command.Parameters.AddWithValue("direction", DbString(GetOptionalString(payload, "direction", "направление")));
        AddNullableGuid(command, "targetLocationId", GetOptionalGuid(payload, "targetLocationId", "target_location_id", "целеваяЛокацияId"));
        command.Parameters.AddWithValue("isLocked", DbBool(GetOptionalBool(payload, "isLocked", "is_locked", "заперт")));
        command.Parameters.AddWithValue("objectType", DbString(GetOptionalString(payload, "objectType", "object_type", "type", "тип")));
        command.Parameters.AddWithValue("state", DbString(GetOptionalString(payload, "state", "состояние")));
        AddNullableGuid(command, "keyItemId", GetOptionalGuid(payload, "keyItemId", "key_item_id", "ключПредметId"));
        command.Parameters.AddWithValue("role", DbString(GetOptionalString(payload, "role", "роль")));
        command.Parameters.AddWithValue("attitude", DbString(GetOptionalString(payload, "attitude", "отношение")));
        command.Parameters.AddWithValue("isAlive", DbBool(GetOptionalBool(payload, "isAlive", "is_alive", "живой")));
        command.Parameters.AddWithValue("reputation", DbInt(GetOptionalInt(payload, "reputation", "репутация")));
        command.Parameters.AddWithValue("title", DbString(GetOptionalString(payload, "title", "name", "название")));
        command.Parameters.AddWithValue("status", DbString(GetOptionalString(payload, "status", "статус")));
        command.Parameters.AddWithValue("rewardExperience", DbInt(GetOptionalInt(payload, "rewardExperience", "reward_experience", "наградаОпыт")));
        command.Parameters.AddWithValue("rewardCopper", DbInt(GetOptionalInt(payload, "rewardCopper", "reward_copper", "наградаМедные")));
        command.Parameters.AddWithValue("rewardSilver", DbInt(GetOptionalInt(payload, "rewardSilver", "reward_silver", "�����������������")));
        command.Parameters.AddWithValue("rewardGold", DbInt(GetOptionalInt(payload, "rewardGold", "reward_gold", "наградаЗолотые")));
        command.Parameters.AddWithValue("rewardPlatinum", DbInt(GetOptionalInt(payload, "rewardPlatinum", "reward_platinum", "наградаПлатиновые")));
        command.Parameters.AddWithValue("isCompleted", DbBool(GetOptionalBool(payload, "isCompleted", "is_completed", "завершён")));
        command.Parameters.AddWithValue("sortOrder", DbInt(GetOptionalInt(payload, "sortOrder", "sort_order", "порядок")));
        command.Parameters.AddWithValue("monsterType", DbString(GetOptionalString(payload, "monsterType", "monster_type", "type", "тип")));
        command.Parameters.AddWithValue("hpCurrent", DbInt(GetOptionalInt(payload, "hpCurrent", "hp_current", "хпТекущее")));
        command.Parameters.AddWithValue("hpMax", DbInt(GetOptionalInt(payload, "hpMax", "hp_max", "хпМаксимум")));
        command.Parameters.AddWithValue("armorClass", DbInt(GetOptionalInt(payload, "armorClass", "armor_class", "классДоспеха")));
        command.Parameters.AddWithValue("initiativeBonus", DbInt(GetOptionalInt(payload, "initiativeBonus", "initiative_bonus", "инициатива")));
        command.Parameters.AddWithValue("status", DbString(GetOptionalString(payload, "status", "статус")));
        command.Parameters.AddWithValue("xpReward", DbInt(GetOptionalInt(payload, "xpReward", "xp_reward", "rewardExperience", "опытНаграда")));
        command.Parameters.AddWithValue("currencyReward", DbInt(GetOptionalInt(payload, "currencyReward", "currency_reward", "rewardGold", "золотоНаграда")));
        AddJsonb(command, "stats", GetOptionalElement(payload, "stats", "статы")?.GetRawText());
        AddJsonb(command, "abilities", GetOptionalElement(payload, "abilities", "способности")?.GetRawText());
        AddJsonb(command, "loot", GetOptionalElement(payload, "loot", "добыча")?.GetRawText());

    }

    private static void EnsureCreateRequired(WorldEntityKind kind, JsonElement payload)
    {
        _ = kind switch
        {
            WorldEntityKind.Location => Require(payload, "name", "название"),
            WorldEntityKind.LocationExit => Require(payload, "direction", "направление") && Require(payload, "targetLocationId", "target_location_id"),
            WorldEntityKind.WorldObject => Require(payload, "name", "название") && Require(payload, "objectType", "object_type", "type", "тип"),
            WorldEntityKind.Container => Require(payload, "name", "название"),
            WorldEntityKind.Npc => Require(payload, "name", "название"),
            WorldEntityKind.Faction => Require(payload, "name", "название"),
            WorldEntityKind.Quest => Require(payload, "title", "name", "название"),
            WorldEntityKind.QuestStep => Require(payload, "description", "описание"),
            WorldEntityKind.Monster => Require(payload, "name", "название"),
            _ => true
        };
    }

    private static bool Require(JsonElement payload, params string[] names)
    {
        if (GetOptionalElement(payload, names).HasValue)
        {
            return true;
        }

        throw new RpgValidationException($"{names[0]} is required.");
    }

    private static async Task<bool> GameStateExistsAsync(NpgsqlConnection connection, Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM game.game_states WHERE id = @gameStateId AND account_id = @accountId);";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task<bool> ExistsAsync(NpgsqlConnection connection, string tableName, Guid gameStateId, Guid id, CancellationToken cancellationToken)
    {
        var sql = $"SELECT EXISTS (SELECT 1 FROM {tableName} WHERE id = @id AND game_state_id = @gameStateId);";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static JsonElement? GetOptionalElement(JsonElement payload, params string[] names)
    {
        if (payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new RpgValidationException("Payload must be a JSON object.");
        }

        foreach (var name in names)
        {
            if (payload.TryGetProperty(name, out var element) && element.ValueKind != JsonValueKind.Null)
            {
                return element;
            }
        }

        return null;
    }

    private static Guid? GetOptionalGuid(JsonElement payload, params string[] names)
    {
        var element = GetOptionalElement(payload, names);
        if (!element.HasValue)
        {
            return null;
        }

        if (element.Value.ValueKind == JsonValueKind.String && Guid.TryParse(element.Value.GetString(), out var id))
        {
            return id;
        }

        throw new RpgValidationException($"{names[0]} must be a UUID.");
    }

    private static string? GetOptionalString(JsonElement payload, params string[] names)
    {
        var element = GetOptionalElement(payload, names);
        return element.HasValue && element.Value.ValueKind == JsonValueKind.String ? element.Value.GetString() : null;
    }

    private static int? GetOptionalInt(JsonElement payload, params string[] names)
    {
        var element = GetOptionalElement(payload, names);
        if (!element.HasValue)
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

    private static bool? GetOptionalBool(JsonElement payload, params string[] names)
    {
        var element = GetOptionalElement(payload, names);
        if (!element.HasValue)
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

    private static object DbString(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static object DbInt(int? value) => value.HasValue ? Math.Max(0, value.Value) : DBNull.Value;

    private static object DbBool(bool? value) => value.HasValue ? value.Value : DBNull.Value;

    private static void AddNullableGuid(NpgsqlCommand command, string name, Guid? value)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Uuid);
        parameter.Value = value.HasValue ? value.Value : DBNull.Value;
    }

    private static void AddJsonb(NpgsqlCommand command, string name, string? json)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Jsonb);
        parameter.Value = string.IsNullOrWhiteSpace(json) ? DBNull.Value : json;
    }
}
