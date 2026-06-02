using System.Text.Json;
using backend.Modules.Characters.Contracts;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Infrastructure.Database;
using Npgsql;

namespace backend.Modules.Characters.Infrastructure;

public sealed class CharacterDomainRepository : ICharacterDomainRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public CharacterDomainRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<JsonElement>?> GetConditionsAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT jsonb_build_object(
                'id', id,
                '��������', name,
                '���', type,
                '��������', description,
                '��������', source,
                '�������������', remaining_turns,
                '����������', is_permanent,
                '�����', stacks,
                '��������������', max_stacks,
                '�������', effects,
                '����', tags
            )::text
            FROM game.conditions
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId
            ORDER BY created_at, id;
        """;

        return await GetCharacterListAsync(accountId, gameStateId, characterId, sql, cancellationToken);
    }

    public async Task<Guid?> CreateConditionAsync(Guid accountId, Guid gameStateId, Guid characterId, ConditionRequest request, CancellationToken cancellationToken)
    {
        ValidateRequired(request.Name, "название состояния обязательно.");
        ValidateRequired(request.Type, "тип состояния обязателен.");

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

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
                @effects::jsonb,
                @tags::jsonb
            )
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        AddConditionParameters(command, gameStateId, characterId, request);
        return (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Condition id was not returned."));
    }

    public Task<bool?> UpdateConditionAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid conditionId, ConditionRequest request, CancellationToken cancellationToken)
    {
        ValidateRequired(request.Name, "название состояния обязательно.");
        ValidateRequired(request.Type, "тип состояния обязателен.");

        const string sql = """
            UPDATE game.conditions
            SET name = @name,
                type = @type,
                description = @description,
                source = @source,
                remaining_turns = @remainingTurns,
                is_permanent = @isPermanent,
                stacks = @stacks,
                max_stacks = @maxStacks,
                effects = @effects::jsonb,
                tags = @tags::jsonb
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId
              AND id = @entityId;
        """;

        return UpdateEntityAsync(accountId, gameStateId, characterId, conditionId, sql, command => AddConditionParameters(command, gameStateId, characterId, request), cancellationToken);
    }

    public Task<bool?> DeleteConditionAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid conditionId, CancellationToken cancellationToken)
        => DeleteEntityAsync(accountId, gameStateId, characterId, conditionId, "game.conditions", cancellationToken);

    public async Task<IReadOnlyList<JsonElement>?> GetLimitedResourcesAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT jsonb_build_object(
                'id', id,
                'название', name,
                'максимум', max_value,
                'текущее', current_value,
                'восстановление', recovery
            )::text
            FROM game.limited_resources
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId
            ORDER BY name, id;
        """;

        return await GetCharacterListAsync(accountId, gameStateId, characterId, sql, cancellationToken);
    }

    public async Task<Guid?> CreateLimitedResourceAsync(Guid accountId, Guid gameStateId, Guid characterId, LimitedResourceRequest request, CancellationToken cancellationToken)
    {
        ValidateLimitedResource(request);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        const string sql = """
            INSERT INTO game.limited_resources
            (
                game_state_id,
                player_id,
                name,
                max_value,
                current_value,
                recovery
            )
            VALUES
            (
                @gameStateId,
                @characterId,
                @name,
                @maxValue,
                @currentValue,
                @recovery
            )
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        AddLimitedResourceParameters(command, gameStateId, characterId, request);
        return (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Limited resource id was not returned."));
    }

    public Task<bool?> UpdateLimitedResourceAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid resourceId, LimitedResourceRequest request, CancellationToken cancellationToken)
    {
        ValidateLimitedResource(request);

        const string sql = """
            UPDATE game.limited_resources
            SET name = @name,
                max_value = @maxValue,
                current_value = @currentValue,
                recovery = @recovery
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId
              AND id = @entityId;
        """;

        return UpdateEntityAsync(accountId, gameStateId, characterId, resourceId, sql, command => AddLimitedResourceParameters(command, gameStateId, characterId, request), cancellationToken);
    }

    public Task<bool?> DeleteLimitedResourceAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid resourceId, CancellationToken cancellationToken)
        => DeleteEntityAsync(accountId, gameStateId, characterId, resourceId, "game.limited_resources", cancellationToken);

    public async Task<IReadOnlyList<JsonElement>?> GetProficienciesAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT jsonb_build_object(
                'id', id,
                'тип', type,
                'значение', value
            )::text
            FROM game.player_proficiencies
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId
            ORDER BY type, value, id;
        """;

        return await GetCharacterListAsync(accountId, gameStateId, characterId, sql, cancellationToken);
    }

    public async Task<Guid?> CreateProficiencyAsync(Guid accountId, Guid gameStateId, Guid characterId, ProficiencyRequest request, CancellationToken cancellationToken)
    {
        ValidateRequired(request.Type, "тип владения обязателен.");
        ValidateRequired(request.Value, "значение владения обязательно.");

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        const string sql = """
            INSERT INTO game.player_proficiencies
            (
                game_state_id,
                player_id,
                type,
                value
            )
            VALUES
            (
                @gameStateId,
                @characterId,
                @type,
                @value
            )
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("type", request.Type.Trim());
        command.Parameters.AddWithValue("value", request.Value.Trim());
        return (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Proficiency id was not returned."));
    }

    public Task<bool?> DeleteProficiencyAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid proficiencyId, CancellationToken cancellationToken)
        => DeleteEntityAsync(accountId, gameStateId, characterId, proficiencyId, "game.player_proficiencies", cancellationToken);

    public async Task<IReadOnlyList<JsonElement>?> GetAbilitiesAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT jsonb_build_object(
                'id', id,
                'категория', category,
                'название', name,
                'описание', description,
                'тип', ability_type,
                'ресурсId', cost_resource_id,
                'стоимость', cost_amount,
                'эффекты', effects
            )::text
            FROM game.abilities
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId
            ORDER BY category, name, id;
        """;

        return await GetCharacterListAsync(accountId, gameStateId, characterId, sql, cancellationToken);
    }

    public async Task<Guid?> CreateAbilityAsync(Guid accountId, Guid gameStateId, Guid characterId, AbilityRequest request, CancellationToken cancellationToken)
    {
        ValidateAbility(request);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        await ValidateLimitedResourceReferenceAsync(connection, gameStateId, characterId, request.CostResourceId, cancellationToken);

        const string sql = """
            INSERT INTO game.abilities
            (
                game_state_id,
                player_id,
                category,
                name,
                description,
                ability_type,
                cost_resource_id,
                cost_amount,
                effects
            )
            VALUES
            (
                @gameStateId,
                @characterId,
                @category,
                @name,
                @description,
                @abilityType,
                @costResourceId,
                @costAmount,
                @effects::jsonb
            )
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        AddAbilityParameters(command, gameStateId, characterId, request);
        return (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Ability id was not returned."));
    }

    public async Task<bool?> UpdateAbilityAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid abilityId, AbilityRequest request, CancellationToken cancellationToken)
    {
        ValidateAbility(request);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        await ValidateLimitedResourceReferenceAsync(connection, gameStateId, characterId, request.CostResourceId, cancellationToken);

        const string sql = """
            UPDATE game.abilities
            SET category = @category,
                name = @name,
                description = @description,
                ability_type = @abilityType,
                cost_resource_id = @costResourceId,
                cost_amount = @costAmount,
                effects = @effects::jsonb
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId
              AND id = @entityId;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("entityId", abilityId);
        AddAbilityParameters(command, gameStateId, characterId, request);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public Task<bool?> DeleteAbilityAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid abilityId, CancellationToken cancellationToken)
        => DeleteEntityAsync(accountId, gameStateId, characterId, abilityId, "game.abilities", cancellationToken);

    public async Task<IReadOnlyList<JsonElement>?> GetInventoryAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT jsonb_build_object(
                'id', id,
                'templateId', template_id,
                'название', name,
                'тип', item_type,
                'подтип', subtype,
                'описание', description,
                'количество', quantity,
                'стакуемый', stackable,
                'вес', weight_each,
                'состояние', condition,
                'редкость', rarity,
                'магический', is_magical,
                'медные', price_copper,
                'серебряные', price_silver,
                'золотые', price_gold,
                'платиновые', price_platinum,
                'теги', tags
            )::text
            FROM game.item_instances
            WHERE game_state_id = @gameStateId
              AND owner_kind = 'player_inventory'
              AND owner_id = @characterId
            ORDER BY name, id;
        """;

        return await GetCharacterListAsync(accountId, gameStateId, characterId, sql, cancellationToken);
    }

    public async Task<Guid?> CreateInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, InventoryItemRequest request, CancellationToken cancellationToken)
    {
        ValidateInventoryItem(request);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

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
                @tags::jsonb,
                'player_inventory',
                @characterId
            )
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        AddInventoryItemParameters(command, gameStateId, characterId, request);
        return (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Item id was not returned."));
    }

    public Task<bool?> UpdateInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, InventoryItemRequest request, CancellationToken cancellationToken)
    {
        ValidateInventoryItem(request);

        const string sql = """
            UPDATE game.item_instances
            SET template_id = @templateId,
                name = @name,
                item_type = @itemType,
                subtype = @subtype,
                description = @description,
                quantity = @quantity,
                stackable = @stackable,
                weight_each = @weightEach,
                condition = @condition,
                rarity = @rarity,
                is_magical = @isMagical,
                price_copper = @priceCopper,
                price_silver = @priceSilver,
                price_gold = @priceGold,
                price_platinum = @pricePlatinum,
                tags = @tags::jsonb
            WHERE id = @entityId
              AND game_state_id = @gameStateId
              AND owner_kind = 'player_inventory'
              AND owner_id = @characterId;
        """;

        return UpdateEntityAsync(accountId, gameStateId, characterId, itemId, sql, command => AddInventoryItemParameters(command, gameStateId, characterId, request), cancellationToken);
    }

    public Task<bool?> DeleteInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM game.item_instances
            WHERE id = @entityId
              AND game_state_id = @gameStateId
              AND owner_kind = 'player_inventory'
              AND owner_id = @characterId;
        """;

        return UpdateEntityAsync(accountId, gameStateId, characterId, itemId, sql, _ => { }, cancellationToken);
    }

    public async Task<bool?> EquipInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, string? slot, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        await EnsureEquipmentAsync(connection, gameStateId, characterId, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var item = await GetInventoryItemForUpdateAsync(connection, transaction, gameStateId, characterId, itemId, cancellationToken);
            if (item is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var normalizedSlot = NormalizeEquipmentSlot(slot ?? InferEquipmentSlot(item.Value.ItemType, item.Value.Tags));
            var slotColumn = GetEquipmentSlotColumn(normalizedSlot);
            var currentItemId = await GetEquippedSlotValueAsync(connection, transaction, gameStateId, characterId, slotColumn, cancellationToken);
            if (currentItemId == itemId)
            {
                await transaction.CommitAsync(cancellationToken);
                return true;
            }

            if (currentItemId.HasValue)
            {
                throw new RpgConflictException($"���� {normalizedSlot} ��� ����� ������ ���������.");
            }

            var sql = $"UPDATE game.equipped_gear SET {slotColumn} = @itemId WHERE game_state_id = @gameStateId AND player_id = @characterId;";
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("gameStateId", gameStateId);
            command.Parameters.AddWithValue("characterId", characterId);
            command.Parameters.AddWithValue("itemId", itemId);
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool?> UnequipInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        if (!await InventoryItemExistsAsync(connection, gameStateId, characterId, itemId, cancellationToken))
        {
            return false;
        }

        await EnsureEquipmentAsync(connection, gameStateId, characterId, cancellationToken);
        await ClearEquipmentItemAsync(connection, gameStateId, characterId, itemId, cancellationToken);
        return true;
    }

    public async Task<bool?> UseInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var item = await GetInventoryItemForUpdateAsync(connection, transaction, gameStateId, characterId, itemId, cancellationToken);
            if (item is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            if (!string.Equals(item.Value.ItemType, "consumable", StringComparison.OrdinalIgnoreCase))
            {
                throw new RpgValidationException("Использовать можно только consumable предмет.");
            }

            if (item.Value.Quantity <= 1)
            {
                await ClearEquipmentItemAsync(connection, transaction, gameStateId, characterId, itemId, cancellationToken);
                const string deleteSql = """
                    DELETE FROM game.item_instances
                    WHERE id = @itemId
                      AND game_state_id = @gameStateId
                      AND owner_kind = 'player_inventory'
                      AND owner_id = @characterId;
                """;
                await using var deleteCommand = new NpgsqlCommand(deleteSql, connection, transaction);
                deleteCommand.Parameters.AddWithValue("gameStateId", gameStateId);
                deleteCommand.Parameters.AddWithValue("characterId", characterId);
                deleteCommand.Parameters.AddWithValue("itemId", itemId);
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
            }
            else
            {
                const string updateSql = """
                    UPDATE game.item_instances
                    SET quantity = quantity - 1
                    WHERE id = @itemId
                      AND game_state_id = @gameStateId
                      AND owner_kind = 'player_inventory'
                      AND owner_id = @characterId;
                """;
                await using var updateCommand = new NpgsqlCommand(updateSql, connection, transaction);
                updateCommand.Parameters.AddWithValue("gameStateId", gameStateId);
                updateCommand.Parameters.AddWithValue("characterId", characterId);
                updateCommand.Parameters.AddWithValue("itemId", itemId);
                await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<JsonElement?> GetEquipmentAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        await EnsureEquipmentAsync(connection, gameStateId, characterId, cancellationToken);

        const string sql = """
            SELECT jsonb_build_object(
                '������', head_item_id,
                '����', body_item_id,
                '����', hands_item_id,
                '����', legs_item_id,
                '�����', feet_item_id,
                '������������', main_hand_item_id,
                '����������', off_hand_item_id,
                '������', amulet_item_id,
                '������1', ring1_item_id,
                '������2', ring2_item_id
            )::text
            FROM game.equipped_gear
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    public async Task<bool?> UpdateEquipmentAsync(Guid accountId, Guid gameStateId, Guid characterId, EquipmentRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        foreach (var itemId in GetEquipmentItemIds(request))
        {
            await ValidateInventoryItemReferenceAsync(connection, gameStateId, characterId, itemId, cancellationToken);
        }

        const string sql = """
            INSERT INTO game.equipped_gear
            (
                player_id,
                game_state_id,
                head_item_id,
                body_item_id,
                hands_item_id,
                legs_item_id,
                feet_item_id,
                main_hand_item_id,
                off_hand_item_id,
                amulet_item_id,
                ring1_item_id,
                ring2_item_id
            )
            VALUES
            (
                @characterId,
                @gameStateId,
                @headItemId,
                @bodyItemId,
                @handsItemId,
                @legsItemId,
                @feetItemId,
                @mainHandItemId,
                @offHandItemId,
                @amuletItemId,
                @ring1ItemId,
                @ring2ItemId
            )
            ON CONFLICT (player_id)
            DO UPDATE SET
                head_item_id = EXCLUDED.head_item_id,
                body_item_id = EXCLUDED.body_item_id,
                hands_item_id = EXCLUDED.hands_item_id,
                legs_item_id = EXCLUDED.legs_item_id,
                feet_item_id = EXCLUDED.feet_item_id,
                main_hand_item_id = EXCLUDED.main_hand_item_id,
                off_hand_item_id = EXCLUDED.off_hand_item_id,
                amulet_item_id = EXCLUDED.amulet_item_id,
                ring1_item_id = EXCLUDED.ring1_item_id,
                ring2_item_id = EXCLUDED.ring2_item_id;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        AddNullableGuid(command, "headItemId", request.HeadItemId);
        AddNullableGuid(command, "bodyItemId", request.BodyItemId);
        AddNullableGuid(command, "handsItemId", request.HandsItemId);
        AddNullableGuid(command, "legsItemId", request.LegsItemId);
        AddNullableGuid(command, "feetItemId", request.FeetItemId);
        AddNullableGuid(command, "mainHandItemId", request.MainHandItemId);
        AddNullableGuid(command, "offHandItemId", request.OffHandItemId);
        AddNullableGuid(command, "amuletItemId", request.AmuletItemId);
        AddNullableGuid(command, "ring1ItemId", request.Ring1ItemId);
        AddNullableGuid(command, "ring2ItemId", request.Ring2ItemId);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<JsonElement>?> GetAttacksAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT jsonb_build_object(
                'id', id,
                'предметId', item_id,
                'название', name,
                'бросок', roll,
                'урон', damage,
                'типУрона', damage_type
            )::text
            FROM game.attacks
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId
            ORDER BY name, id;
        """;

        return await GetCharacterListAsync(accountId, gameStateId, characterId, sql, cancellationToken);
    }

    public async Task<Guid?> CreateAttackAsync(Guid accountId, Guid gameStateId, Guid characterId, AttackRequest request, CancellationToken cancellationToken)
    {
        ValidateAttack(request);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        await ValidateInventoryItemReferenceAsync(connection, gameStateId, characterId, request.ItemId, cancellationToken);

        const string sql = """
            INSERT INTO game.attacks
            (
                game_state_id,
                player_id,
                item_id,
                name,
                roll,
                damage,
                damage_type
            )
            VALUES
            (
                @gameStateId,
                @characterId,
                @itemId,
                @name,
                @roll,
                @damage,
                @damageType
            )
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        AddAttackParameters(command, gameStateId, characterId, request);
        return (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Attack id was not returned."));
    }

    public async Task<bool?> UpdateAttackAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid attackId, AttackRequest request, CancellationToken cancellationToken)
    {
        ValidateAttack(request);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        await ValidateInventoryItemReferenceAsync(connection, gameStateId, characterId, request.ItemId, cancellationToken);

        const string sql = """
            UPDATE game.attacks
            SET item_id = @itemId,
                name = @name,
                roll = @roll,
                damage = @damage,
                damage_type = @damageType
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId
              AND id = @entityId;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("entityId", attackId);
        AddAttackParameters(command, gameStateId, characterId, request);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public Task<bool?> DeleteAttackAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid attackId, CancellationToken cancellationToken)
        => DeleteEntityAsync(accountId, gameStateId, characterId, attackId, "game.attacks", cancellationToken);

    public async Task<JsonElement?> GetNeedsAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        await EnsureNeedsAsync(connection, gameStateId, characterId, cancellationToken);

        const string sql = """
            SELECT jsonb_build_object(
                'размерЕды', food_size,
                'едыВДень', food_per_day,
                'едыОсталось', food_remaining,
                'размерВоды', water_size,
                'водыВДень', water_per_day,
                'водыОсталось', water_remaining,
                'грузМаксимум', carry_capacity_max,
                'текущийВес', carry_current_weight,
                'единицаВеса', carry_unit,
                'метровЗаХод', movement_meters_per_turn,
                'кмВДень', movement_km_per_day
            )::text
            FROM game.player_needs
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    public async Task<bool?> UpdateNeedsAsync(Guid accountId, Guid gameStateId, Guid characterId, NeedsRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        const string sql = """
            INSERT INTO game.player_needs
            (
                player_id,
                game_state_id,
                food_size,
                food_per_day,
                food_remaining,
                water_size,
                water_per_day,
                water_remaining,
                carry_capacity_max,
                carry_current_weight,
                carry_unit,
                movement_meters_per_turn,
                movement_km_per_day
            )
            VALUES
            (
                @characterId,
                @gameStateId,
                @foodSize,
                @foodPerDay,
                @foodRemaining,
                @waterSize,
                @waterPerDay,
                @waterRemaining,
                @carryCapacityMax,
                @carryCurrentWeight,
                @carryUnit,
                @movementMetersPerTurn,
                @movementKmPerDay
            )
            ON CONFLICT (player_id)
            DO UPDATE SET
                food_size = EXCLUDED.food_size,
                food_per_day = EXCLUDED.food_per_day,
                food_remaining = EXCLUDED.food_remaining,
                water_size = EXCLUDED.water_size,
                water_per_day = EXCLUDED.water_per_day,
                water_remaining = EXCLUDED.water_remaining,
                carry_capacity_max = EXCLUDED.carry_capacity_max,
                carry_current_weight = EXCLUDED.carry_current_weight,
                carry_unit = EXCLUDED.carry_unit,
                movement_meters_per_turn = EXCLUDED.movement_meters_per_turn,
                movement_km_per_day = EXCLUDED.movement_km_per_day;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("foodSize", string.IsNullOrWhiteSpace(request.FoodSize) ? "средний" : request.FoodSize.Trim());
        command.Parameters.AddWithValue("foodPerDay", RpgDbJson.DbString(request.FoodPerDay));
        command.Parameters.AddWithValue("foodRemaining", Math.Max(0, request.FoodRemaining));
        command.Parameters.AddWithValue("waterSize", string.IsNullOrWhiteSpace(request.WaterSize) ? "средний" : request.WaterSize.Trim());
        command.Parameters.AddWithValue("waterPerDay", RpgDbJson.DbString(request.WaterPerDay));
        command.Parameters.AddWithValue("waterRemaining", Math.Max(0, request.WaterRemaining));
        command.Parameters.AddWithValue("carryCapacityMax", Math.Max(0, request.CarryCapacityMax));
        command.Parameters.AddWithValue("carryCurrentWeight", Math.Max(0, request.CarryCurrentWeight));
        command.Parameters.AddWithValue("carryUnit", string.IsNullOrWhiteSpace(request.CarryUnit) ? "кг" : request.CarryUnit.Trim());
        command.Parameters.AddWithValue("movementMetersPerTurn", Math.Max(0, request.MovementMetersPerTurn));
        command.Parameters.AddWithValue("movementKmPerDay", Math.Max(0, request.MovementKmPerDay));
        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public Task<bool?> UpdateProgressionAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterProgressionRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.player_progression (player_id, game_state_id, level, experience, experience_to_next_level)
            VALUES (@characterId, @gameStateId, @level, @experience, @experienceToNextLevel)
            ON CONFLICT (player_id)
            DO UPDATE SET
                level = EXCLUDED.level,
                experience = EXCLUDED.experience,
                experience_to_next_level = EXCLUDED.experience_to_next_level;
        """;

        return UpsertSingleRowAsync(accountId, gameStateId, characterId, sql, command =>
        {
            command.Parameters.AddWithValue("level", Math.Max(1, request.Level));
            command.Parameters.AddWithValue("experience", Math.Max(0, request.Experience));
            command.Parameters.AddWithValue("experienceToNextLevel", Math.Max(0, request.ExperienceToNextLevel));
        }, cancellationToken);
    }

    public Task<bool?> UpdateResourcesAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterResourcesRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.player_resources
            (
                player_id,
                game_state_id,
                hp_max,
                hp_current,
                mana_max,
                mana_current,
                action_points_max,
                action_points_current
            )
            VALUES
            (
                @characterId,
                @gameStateId,
                @hpMax,
                @hpCurrent,
                @manaMax,
                @manaCurrent,
                @actionPointsMax,
                @actionPointsCurrent
            )
            ON CONFLICT (player_id)
            DO UPDATE SET
                hp_max = EXCLUDED.hp_max,
                hp_current = EXCLUDED.hp_current,
                mana_max = EXCLUDED.mana_max,
                mana_current = EXCLUDED.mana_current,
                action_points_max = EXCLUDED.action_points_max,
                action_points_current = EXCLUDED.action_points_current;
        """;

        return UpsertSingleRowAsync(accountId, gameStateId, characterId, sql, command =>
        {
            command.Parameters.AddWithValue("hpMax", Math.Max(0, request.HpMax));
            command.Parameters.AddWithValue("hpCurrent", Math.Max(0, request.HpCurrent));
            command.Parameters.AddWithValue("manaMax", Math.Max(0, request.ManaMax));
            command.Parameters.AddWithValue("manaCurrent", Math.Max(0, request.ManaCurrent));
            command.Parameters.AddWithValue("actionPointsMax", Math.Max(0, request.ActionPointsMax));
            command.Parameters.AddWithValue("actionPointsCurrent", Math.Max(0, request.ActionPointsCurrent));
        }, cancellationToken);
    }

    public Task<bool?> UpdateAttributesAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterAttributesRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.player_attributes
            (
                player_id,
                game_state_id,
                strength,
                dexterity,
                constitution,
                intelligence,
                wisdom,
                charisma,
                initiative,
                speed,
                perception
            )
            VALUES
            (
                @characterId,
                @gameStateId,
                @strength,
                @dexterity,
                @constitution,
                @intelligence,
                @wisdom,
                @charisma,
                @initiative,
                @speed,
                @perception
            )
            ON CONFLICT (player_id)
            DO UPDATE SET
                strength = EXCLUDED.strength,
                dexterity = EXCLUDED.dexterity,
                constitution = EXCLUDED.constitution,
                intelligence = EXCLUDED.intelligence,
                wisdom = EXCLUDED.wisdom,
                charisma = EXCLUDED.charisma,
                initiative = EXCLUDED.initiative,
                speed = EXCLUDED.speed,
                perception = EXCLUDED.perception;
        """;

        return UpsertSingleRowAsync(accountId, gameStateId, characterId, sql, command =>
        {
            command.Parameters.AddWithValue("strength", request.Strength);
            command.Parameters.AddWithValue("dexterity", request.Dexterity);
            command.Parameters.AddWithValue("constitution", request.Constitution);
            command.Parameters.AddWithValue("intelligence", request.Intelligence);
            command.Parameters.AddWithValue("wisdom", request.Wisdom);
            command.Parameters.AddWithValue("charisma", request.Charisma);
            command.Parameters.AddWithValue("initiative", request.Initiative);
            command.Parameters.AddWithValue("speed", Math.Max(0, request.Speed));
            command.Parameters.AddWithValue("perception", request.Perception);
        }, cancellationToken);
    }

    public Task<bool?> UpdateWealthAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterWealthRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.wealth (player_id, game_state_id, copper, silver, gold, platinum)
            VALUES (@characterId, @gameStateId, @copper, @silver, @gold, @platinum)
            ON CONFLICT (player_id)
            DO UPDATE SET
                copper = EXCLUDED.copper,
                silver = EXCLUDED.silver,
                gold = EXCLUDED.gold,
                platinum = EXCLUDED.platinum;
        """;

        return UpsertSingleRowAsync(accountId, gameStateId, characterId, sql, command =>
        {
            command.Parameters.AddWithValue("copper", Math.Max(0, request.Copper));
            command.Parameters.AddWithValue("silver", Math.Max(0, request.Silver));
            command.Parameters.AddWithValue("gold", Math.Max(0, request.Gold));
            command.Parameters.AddWithValue("platinum", Math.Max(0, request.Platinum));
        }, cancellationToken);
    }

    public Task<bool?> UpdateCombatStatsAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterCombatStatsRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.combat_stats
            (
                player_id,
                game_state_id,
                armor_class,
                proficiency_bonus,
                in_combat,
                initiative_roll
            )
            VALUES
            (
                @characterId,
                @gameStateId,
                @armorClass,
                @proficiencyBonus,
                @inCombat,
                @initiativeRoll
            )
            ON CONFLICT (player_id)
            DO UPDATE SET
                armor_class = EXCLUDED.armor_class,
                proficiency_bonus = EXCLUDED.proficiency_bonus,
                in_combat = EXCLUDED.in_combat,
                initiative_roll = EXCLUDED.initiative_roll;
        """;

        return UpsertSingleRowAsync(accountId, gameStateId, characterId, sql, command =>
        {
            command.Parameters.AddWithValue("armorClass", Math.Max(0, request.ArmorClass));
            command.Parameters.AddWithValue("proficiencyBonus", Math.Max(0, request.ProficiencyBonus));
            command.Parameters.AddWithValue("inCombat", request.InCombat);
            command.Parameters.AddWithValue("initiativeRoll", request.InitiativeRoll);
        }, cancellationToken);
    }

    private async Task<IReadOnlyList<JsonElement>?> GetCharacterListAsync(
        Guid accountId,
        Guid gameStateId,
        Guid characterId,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<JsonElement>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(RpgDbJson.ParseElement(reader.GetString(0)));
        }

        return result;
    }

    private async Task<bool?> UpdateEntityAsync(
        Guid accountId,
        Guid gameStateId,
        Guid characterId,
        Guid entityId,
        string sql,
        Action<NpgsqlCommand> configure,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("entityId", entityId);
        configure(command);
        if (!command.Parameters.Contains("gameStateId"))
        {
            command.Parameters.AddWithValue("gameStateId", gameStateId);
        }

        if (!command.Parameters.Contains("characterId"))
        {
            command.Parameters.AddWithValue("characterId", characterId);
        }

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private Task<bool?> DeleteEntityAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid entityId, string tableName, CancellationToken cancellationToken)
    {
        var sql = $"""
            DELETE FROM {tableName}
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId
              AND id = @entityId;
        """;

        return UpdateEntityAsync(accountId, gameStateId, characterId, entityId, sql, _ => { }, cancellationToken);
    }

    private async Task<bool?> UpsertSingleRowAsync(
        Guid accountId,
        Guid gameStateId,
        Guid characterId,
        string sql,
        Action<NpgsqlCommand> configure,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await CharacterExistsAsync(connection, accountId, gameStateId, characterId, cancellationToken))
        {
            return null;
        }

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        configure(command);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static async Task<bool> CharacterExistsAsync(
        NpgsqlConnection connection,
        Guid accountId,
        Guid gameStateId,
        Guid characterId,
        CancellationToken cancellationToken)
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

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task ValidateLimitedResourceReferenceAsync(
        NpgsqlConnection connection,
        Guid gameStateId,
        Guid characterId,
        Guid? resourceId,
        CancellationToken cancellationToken)
    {
        if (!resourceId.HasValue)
        {
            return;
        }

        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM game.limited_resources
                WHERE id = @resourceId
                  AND game_state_id = @gameStateId
                  AND player_id = @characterId
            );
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("resourceId", resourceId.Value);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        if (await command.ExecuteScalarAsync(cancellationToken) is not true)
        {
            throw new RpgValidationException("������ ����������� �� ����������� ����� ���������.");
        }
    }

    private static async Task ValidateInventoryItemReferenceAsync(
        NpgsqlConnection connection,
        Guid gameStateId,
        Guid characterId,
        Guid? itemId,
        CancellationToken cancellationToken)
    {
        if (!itemId.HasValue)
        {
            return;
        }

        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM game.item_instances
                WHERE id = @itemId
                  AND game_state_id = @gameStateId
                  AND owner_kind = 'player_inventory'
                  AND owner_id = @characterId
            );
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("itemId", itemId.Value);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        if (await command.ExecuteScalarAsync(cancellationToken) is not true)
        {
            throw new RpgValidationException("Предмет не принадлежит инвентарю этого персонажа.");
        }
    }

    private static async Task EnsureEquipmentAsync(NpgsqlConnection connection, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.equipped_gear (player_id, game_state_id)
            VALUES (@characterId, @gameStateId)
            ON CONFLICT (player_id) DO NOTHING;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureNeedsAsync(NpgsqlConnection connection, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.player_needs (player_id, game_state_id)
            VALUES (@characterId, @gameStateId)
            ON CONFLICT (player_id) DO NOTHING;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<InventoryItemRow?> GetInventoryItemForUpdateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        Guid characterId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT item_type, quantity, tags::text
            FROM game.item_instances
            WHERE id = @itemId
              AND game_state_id = @gameStateId
              AND owner_kind = 'player_inventory'
              AND owner_id = @characterId
            FOR UPDATE;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("itemId", itemId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new InventoryItemRow(
            reader.GetString(0),
            reader.GetInt32(1),
            RpgDbJson.ParseElement(reader.GetString(2)));
    }

    private static async Task<bool> InventoryItemExistsAsync(NpgsqlConnection connection, Guid gameStateId, Guid characterId, Guid itemId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM game.item_instances
                WHERE id = @itemId
                  AND game_state_id = @gameStateId
                  AND owner_kind = 'player_inventory'
                  AND owner_id = @characterId
            );
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("itemId", itemId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task<Guid?> GetEquippedSlotValueAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        Guid characterId,
        string slotColumn,
        CancellationToken cancellationToken)
    {
        var sql = $"SELECT {slotColumn} FROM game.equipped_gear WHERE game_state_id = @gameStateId AND player_id = @characterId FOR UPDATE;";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : (Guid)value;
    }

    private static Task ClearEquipmentItemAsync(NpgsqlConnection connection, Guid gameStateId, Guid characterId, Guid itemId, CancellationToken cancellationToken)
        => ClearEquipmentItemAsync(connection, null, gameStateId, characterId, itemId, cancellationToken);

    private static async Task ClearEquipmentItemAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid gameStateId,
        Guid characterId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        const string sql = """
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
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId;
        """;

        await using var command = transaction is null
            ? new NpgsqlCommand(sql, connection)
            : new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("itemId", itemId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string InferEquipmentSlot(string itemType, JsonElement tags)
    {
        if (tags.ValueKind == JsonValueKind.Object)
        {
            foreach (var propertyName in new[] { "slot", "слот" })
            {
                if (tags.TryGetProperty(propertyName, out var slotElement)
                    && slotElement.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(slotElement.GetString()))
                {
                    return slotElement.GetString()!;
                }
            }
        }

        return itemType.Trim().ToLowerInvariant() switch
        {
            "weapon" => "main_hand",
            "armor" => "body",
            _ => throw new RpgValidationException("slot обязателен для экипировки этого предмета.")
        };
    }

    private static string NormalizeEquipmentSlot(string slot)
    {
        return slot.Trim().ToLowerInvariant() switch
        {
            "head" or "голова" => "head",
            "body" or "тело" => "body",
            "hands" or "руки" => "hands",
            "legs" or "ноги" => "legs",
            "feet" or "обувь" => "feet",
            "main_hand" or "mainhand" or "main-hand" or "основнаярука" or "основная_рука" => "main_hand",
            "off_hand" or "offhand" or "off-hand" or "втораярука" or "вторая_рука" => "off_hand",
            "amulet" or "амулет" => "amulet",
            "ring1" or "ring_1" or "кольцо1" => "ring1",
            "ring2" or "ring_2" or "кольцо2" => "ring2",
            _ => throw new RpgValidationException("slot экипировки не поддерживается.")
        };
    }

    private static string GetEquipmentSlotColumn(string slot)
        => slot switch
        {
            "head" => "head_item_id",
            "body" => "body_item_id",
            "hands" => "hands_item_id",
            "legs" => "legs_item_id",
            "feet" => "feet_item_id",
            "main_hand" => "main_hand_item_id",
            "off_hand" => "off_hand_item_id",
            "amulet" => "amulet_item_id",
            "ring1" => "ring1_item_id",
            "ring2" => "ring2_item_id",
            _ => throw new RpgValidationException("slot экипировки не поддерживается.")
        };

    private static string ResolveInventoryTagsJson(InventoryItemRequest request)
    {
        if (request.Tags.HasValue && request.Tags.Value.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
        {
            return request.Tags.Value.GetRawText();
        }

        if (string.IsNullOrWhiteSpace(request.ResolvedSlot) && !request.ResolvedProperties.HasValue)
        {
            return "[]";
        }

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["slot"] = request.ResolvedSlot
        };

        if (request.ResolvedProperties.HasValue && request.ResolvedProperties.Value.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
        {
            payload["properties"] = request.ResolvedProperties.Value;
        }

        return JsonSerializer.Serialize(payload);
    }

    private static void AddConditionParameters(NpgsqlCommand command, Guid gameStateId, Guid characterId, ConditionRequest request)
    {
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("name", request.Name.Trim());
        command.Parameters.AddWithValue("type", request.Type.Trim());
        command.Parameters.AddWithValue("description", RpgDbJson.DbString(request.Description));
        command.Parameters.AddWithValue("source", RpgDbJson.DbString(request.Source));
        command.Parameters.AddWithValue("remainingTurns", request.RemainingTurns.HasValue ? request.RemainingTurns.Value : DBNull.Value);
        command.Parameters.AddWithValue("isPermanent", request.IsPermanent);
        command.Parameters.AddWithValue("stacks", Math.Max(1, request.Stacks));
        command.Parameters.AddWithValue("maxStacks", request.MaxStacks.HasValue ? Math.Max(1, request.MaxStacks.Value) : DBNull.Value);
        command.Parameters.AddWithValue("effects", JsonOrDefault(request.Effects, "[]"));
        command.Parameters.AddWithValue("tags", JsonOrDefault(request.Tags, "[]"));
    }

    private static void AddLimitedResourceParameters(NpgsqlCommand command, Guid gameStateId, Guid characterId, LimitedResourceRequest request)
    {
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("name", request.Name.Trim());
        command.Parameters.AddWithValue("maxValue", Math.Max(0, request.MaxValue));
        command.Parameters.AddWithValue("currentValue", Math.Max(0, request.CurrentValue));
        command.Parameters.AddWithValue("recovery", string.IsNullOrWhiteSpace(request.Recovery) ? "rest" : request.Recovery.Trim());
    }

    private static void AddAbilityParameters(NpgsqlCommand command, Guid gameStateId, Guid characterId, AbilityRequest request)
    {
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("category", request.Category.Trim());
        command.Parameters.AddWithValue("name", request.Name.Trim());
        command.Parameters.AddWithValue("description", RpgDbJson.DbString(request.Description));
        command.Parameters.AddWithValue("abilityType", string.IsNullOrWhiteSpace(request.AbilityType) ? "active" : request.AbilityType.Trim());
        AddNullableGuid(command, "costResourceId", request.CostResourceId);
        command.Parameters.AddWithValue("costAmount", request.CostAmount.HasValue ? Math.Max(0, request.CostAmount.Value) : DBNull.Value);
        command.Parameters.AddWithValue("effects", JsonOrDefault(request.Effects, "[]"));
    }

    private static void AddInventoryItemParameters(NpgsqlCommand command, Guid gameStateId, Guid characterId, InventoryItemRequest request)
    {
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("templateId", RpgDbJson.DbString(request.TemplateId ?? request.TemplateIdSnake));
        command.Parameters.AddWithValue("name", (request.ResolvedName ?? string.Empty).Trim());
        command.Parameters.AddWithValue("itemType", (request.ResolvedItemType ?? string.Empty).Trim());
        command.Parameters.AddWithValue("subtype", RpgDbJson.DbString(request.Subtype ?? request.SubtypeAlias));
        command.Parameters.AddWithValue("description", RpgDbJson.DbString(request.Description ?? request.DescriptionAlias));
        command.Parameters.AddWithValue("quantity", Math.Max(0, request.ResolvedQuantity));
        command.Parameters.AddWithValue("stackable", request.Stackable || request.StackableAlias);
        command.Parameters.AddWithValue("weightEach", Math.Max(0, request.ResolvedWeightEach));
        command.Parameters.AddWithValue("condition", string.IsNullOrWhiteSpace(request.Condition ?? request.ConditionAlias) ? "normal" : (request.Condition ?? request.ConditionAlias)!.Trim());
        command.Parameters.AddWithValue("rarity", string.IsNullOrWhiteSpace(request.Rarity ?? request.RarityAlias) ? "common" : (request.Rarity ?? request.RarityAlias)!.Trim());
        command.Parameters.AddWithValue("isMagical", request.IsMagical || request.IsMagicalSnake);
        command.Parameters.AddWithValue("priceCopper", Math.Max(0, request.PriceCopper != 0 ? request.PriceCopper : request.PriceCopperSnake));
        command.Parameters.AddWithValue("priceSilver", Math.Max(0, request.PriceSilver != 0 ? request.PriceSilver : request.PriceSilverSnake));
        command.Parameters.AddWithValue("priceGold", Math.Max(0, request.PriceGold != 0 ? request.PriceGold : request.PriceGoldSnake));
        command.Parameters.AddWithValue("pricePlatinum", Math.Max(0, request.PricePlatinum != 0 ? request.PricePlatinum : request.PricePlatinumSnake));
        command.Parameters.AddWithValue("tags", ResolveInventoryTagsJson(request));
    }

    private static void AddAttackParameters(NpgsqlCommand command, Guid gameStateId, Guid characterId, AttackRequest request)
    {
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        AddNullableGuid(command, "itemId", request.ItemId);
        command.Parameters.AddWithValue("name", request.Name.Trim());
        command.Parameters.AddWithValue("roll", request.Roll.Trim());
        command.Parameters.AddWithValue("damage", request.Damage.Trim());
        command.Parameters.AddWithValue("damageType", RpgDbJson.DbString(request.DamageType));
    }

    private static IEnumerable<Guid> GetEquipmentItemIds(EquipmentRequest request)
    {
        var items = new[]
        {
            request.HeadItemId,
            request.BodyItemId,
            request.HandsItemId,
            request.LegsItemId,
            request.FeetItemId,
            request.MainHandItemId,
            request.OffHandItemId,
            request.AmuletItemId,
            request.Ring1ItemId,
            request.Ring2ItemId
        };

        return items.Where(item => item.HasValue).Select(item => item!.Value);
    }

    private static void ValidateLimitedResource(LimitedResourceRequest request)
    {
        ValidateRequired(request.Name, "название ресурса обязательно.");
        ValidateRequired(request.Recovery, "тип восстановления обязателен.");
        if (request.CurrentValue > request.MaxValue)
        {
            throw new RpgValidationException("текущее значение ресурса не может быть больше максимума.");
        }
    }

    private static void ValidateAbility(AbilityRequest request)
    {
        ValidateRequired(request.Category, "категория способности обязательна.");
        ValidateRequired(request.Name, "название способности обязательно.");
        ValidateRequired(request.AbilityType, "тип способности обязателен.");
    }

    private static void ValidateInventoryItem(InventoryItemRequest request)
    {
        ValidateRequired(request.ResolvedName, "название предмета обязательно.");
        ValidateRequired(request.ResolvedItemType, "тип предмета обязателен.");
        if (request.ResolvedQuantity < 1)
        {
            throw new RpgValidationException("количество предмета должно быть не меньше 1.");
        }
    }

    private static void ValidateAttack(AttackRequest request)
    {
        ValidateRequired(request.Name, "название атаки обязательно.");
        ValidateRequired(request.Roll, "бросок атаки обязателен.");
        ValidateRequired(request.Damage, "урон атаки обязателен.");
    }

    private static void ValidateRequired(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new RpgValidationException(message);
        }
    }

    private static void AddNullableGuid(NpgsqlCommand command, string name, Guid? value)
    {
        command.Parameters.AddWithValue(name, value.HasValue ? value.Value : DBNull.Value);
    }

    private static string JsonOrDefault(JsonElement? value, string defaultJson)
    {
        return value.HasValue && value.Value.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null
            ? value.Value.GetRawText()
            : defaultJson;
    }

    private readonly record struct InventoryItemRow(string ItemType, int Quantity, JsonElement Tags);
}
