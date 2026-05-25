using System.Text.Json;
using backend.Models;
using Npgsql;

namespace backend.Services
{
    public class PlayerDatabaseService
    {
        private readonly string _connectionString;

        public PlayerDatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DndDatabase")
                ?? throw new InvalidOperationException("Connection string 'DndDatabase' not found.");
        }

        public async Task<List<Player>> GetPlayersAsync()
        {
            var players = new List<Player>();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
                SELECT data
                FROM game.player_documents
                ORDER BY data->'персонаж'->>'имя';
            """;

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var json = reader.GetString(0);

                var player = JsonSerializer.Deserialize<Player>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );

                if (player != null)
                {
                    players.Add(player);
                }
            }

            return players;
        }

        public async Task<Player?> GetPlayerByIdAsync(Guid id)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
                SELECT data
                FROM game.player_documents
                WHERE player_id = @id;
            """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", id);

            var result = await command.ExecuteScalarAsync();

            if (result == null)
            {
                return null;
            }

            var json = result.ToString();

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<Player>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );
        }

        public async Task<Guid> CreatePlayerAsync(Player player)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                const string insertPlayerSql = """
                    INSERT INTO game.players
                    (
                        name,
                        background,
                        species,
                        class_name,
                        subclass,
                        level,
                        experience
                    )
                    VALUES
                    (
                        @name,
                        @background,
                        @species,
                        @className,
                        @subclass,
                        @level,
                        @experience
                    )
                    RETURNING id;
                """;

                await using var insertPlayerCommand = new NpgsqlCommand(insertPlayerSql, connection, transaction);

                insertPlayerCommand.Parameters.AddWithValue("name", player.Character.Name);
                insertPlayerCommand.Parameters.AddWithValue("background", player.Character.Background);
                insertPlayerCommand.Parameters.AddWithValue("species", player.Character.Species);
                insertPlayerCommand.Parameters.AddWithValue("className", player.Character.Class);
                insertPlayerCommand.Parameters.AddWithValue("subclass", player.Character.Subclass);
                insertPlayerCommand.Parameters.AddWithValue("level", player.Character.Level);
                insertPlayerCommand.Parameters.AddWithValue("experience", player.Character.Experience);

                var playerId = (Guid)(await insertPlayerCommand.ExecuteScalarAsync()
                    ?? throw new InvalidOperationException("Could not create player."));

                await UpdateResourcesAsync(connection, transaction, playerId, player);
                await UpdateAttributesAsync(connection, transaction, playerId, player);
                await UpdateNeedsAsync(connection, transaction, playerId, player);
                await UpdateMovementAsync(connection, transaction, playerId, player);
                await UpdateWealthAsync(connection, transaction, playerId, player);
                await UpdateEquipmentProficienciesAsync(connection, transaction, playerId, player);
                await UpdateEquippedGearAsync(connection, transaction, playerId, player);
                await UpdateCombatStatsAsync(connection, transaction, playerId, player);

                await InsertConditionsAsync(connection, transaction, playerId, player);
                await InsertAbilitiesAsync(connection, transaction, playerId, player);
                await InsertInventoryAsync(connection, transaction, playerId, player);
                await InsertOtherWeaponProficienciesAsync(connection, transaction, playerId, player);
                await InsertAttacksAsync(connection, transaction, playerId, player);

                await transaction.CommitAsync();

                return playerId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static async Task UpdateResourcesAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            const string sql = """
                UPDATE game.resources
                SET
                    hp_max = @hpMax,
                    hp_current = @hpCurrent,
                    mana_max = @manaMax,
                    mana_current = @manaCurrent,
                    action_points_max = @actionPointsMax,
                    action_points_current = @actionPointsCurrent
                WHERE player_id = @playerId;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);

            command.Parameters.AddWithValue("playerId", playerId);
            command.Parameters.AddWithValue("hpMax", player.Resources.Hp.Max);
            command.Parameters.AddWithValue("hpCurrent", player.Resources.Hp.Current);
            command.Parameters.AddWithValue("manaMax", player.Resources.Mana.Max);
            command.Parameters.AddWithValue("manaCurrent", player.Resources.Mana.Current);
            command.Parameters.AddWithValue("actionPointsMax", player.Resources.ActionPoints.Max);
            command.Parameters.AddWithValue("actionPointsCurrent", player.Resources.ActionPoints.Current);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task UpdateAttributesAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            const string sql = """
                UPDATE game.attributes
                SET
                    strength = @strength,
                    intelligence = @intelligence,
                    dexterity = @dexterity,
                    wisdom = @wisdom,
                    constitution = @constitution,
                    charisma = @charisma,
                    initiative = @initiative,
                    speed = @speed,
                    perception = @perception
                WHERE player_id = @playerId;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);

            command.Parameters.AddWithValue("playerId", playerId);
            command.Parameters.AddWithValue("strength", player.Attributes.Strength);
            command.Parameters.AddWithValue("intelligence", player.Attributes.Intelligence);
            command.Parameters.AddWithValue("dexterity", player.Attributes.Dexterity);
            command.Parameters.AddWithValue("wisdom", player.Attributes.Wisdom);
            command.Parameters.AddWithValue("constitution", player.Attributes.Constitution);
            command.Parameters.AddWithValue("charisma", player.Attributes.Charisma);
            command.Parameters.AddWithValue("initiative", player.Attributes.Initiative);
            command.Parameters.AddWithValue("speed", player.Attributes.Speed);
            command.Parameters.AddWithValue("perception", player.Attributes.Perception);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task UpdateNeedsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            const string sql = """
                UPDATE game.needs
                SET
                    food_size = @foodSize,
                    food_per_day = @foodPerDay,
                    water_size = @waterSize,
                    water_per_day = @waterPerDay,
                    carrying_capacity_max = @carryingCapacityMax,
                    carrying_capacity_unit = @carryingCapacityUnit
                WHERE player_id = @playerId;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);

            command.Parameters.AddWithValue("playerId", playerId);
            command.Parameters.AddWithValue("foodSize", player.Needs.Food.Size);
            command.Parameters.AddWithValue("foodPerDay", player.Needs.Food.PerDay);
            command.Parameters.AddWithValue("waterSize", player.Needs.Water.Size);
            command.Parameters.AddWithValue("waterPerDay", player.Needs.Water.PerDay);
            command.Parameters.AddWithValue("carryingCapacityMax", player.Needs.CarryingCapacity.Max);
            command.Parameters.AddWithValue("carryingCapacityUnit", player.Needs.CarryingCapacity.Unit);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task UpdateMovementAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            const string sql = """
                UPDATE game.movement
                SET km_per_turn = @kmPerTurn
                WHERE player_id = @playerId;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);

            command.Parameters.AddWithValue("playerId", playerId);
            command.Parameters.AddWithValue("kmPerTurn", player.Movement.KmPerTurn);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task UpdateWealthAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            const string sql = """
                UPDATE game.wealth
                SET
                    copper = @copper,
                    silver = @silver,
                    gold = @gold,
                    platinum = @platinum
                WHERE player_id = @playerId;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);

            command.Parameters.AddWithValue("playerId", playerId);
            command.Parameters.AddWithValue("copper", player.Wealth.Coins.Copper);
            command.Parameters.AddWithValue("silver", player.Wealth.Coins.Silver);
            command.Parameters.AddWithValue("gold", player.Wealth.Coins.Gold);
            command.Parameters.AddWithValue("platinum", player.Wealth.Coins.Platinum);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task UpdateEquipmentProficienciesAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            const string sql = """
                UPDATE game.equipment_proficiencies
                SET
                    armor_light = @armorLight,
                    armor_medium = @armorMedium,
                    armor_heavy = @armorHeavy,
                    armor_shields = @armorShields,
                    weapon_simple = @weaponSimple,
                    weapon_martial = @weaponMartial
                WHERE player_id = @playerId;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);

            command.Parameters.AddWithValue("playerId", playerId);
            command.Parameters.AddWithValue("armorLight", player.EquipmentProficiency.Armor.Light);
            command.Parameters.AddWithValue("armorMedium", player.EquipmentProficiency.Armor.Medium);
            command.Parameters.AddWithValue("armorHeavy", player.EquipmentProficiency.Armor.Heavy);
            command.Parameters.AddWithValue("armorShields", player.EquipmentProficiency.Armor.Shields);
            command.Parameters.AddWithValue("weaponSimple", player.EquipmentProficiency.Weapons.Simple);
            command.Parameters.AddWithValue("weaponMartial", player.EquipmentProficiency.Weapons.Martial);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task UpdateEquippedGearAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            const string sql = """
                UPDATE game.equipped_gear
                SET
                    head = @head,
                    body = @body,
                    hands = @hands,
                    legs = @legs,
                    feet = @feet,
                    main_hand = @mainHand,
                    off_hand = @offHand,
                    amulet = @amulet,
                    ring_1 = @ring1,
                    ring_2 = @ring2
                WHERE player_id = @playerId;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);

            command.Parameters.AddWithValue("playerId", playerId);
            command.Parameters.AddWithValue("head", (object?)player.EquippedGear.Head ?? DBNull.Value);
            command.Parameters.AddWithValue("body", (object?)player.EquippedGear.Body ?? DBNull.Value);
            command.Parameters.AddWithValue("hands", (object?)player.EquippedGear.Hands ?? DBNull.Value);
            command.Parameters.AddWithValue("legs", (object?)player.EquippedGear.Legs ?? DBNull.Value);
            command.Parameters.AddWithValue("feet", (object?)player.EquippedGear.Feet ?? DBNull.Value);
            command.Parameters.AddWithValue("mainHand", (object?)player.EquippedGear.MainHand ?? DBNull.Value);
            command.Parameters.AddWithValue("offHand", (object?)player.EquippedGear.OffHand ?? DBNull.Value);
            command.Parameters.AddWithValue("amulet", (object?)player.EquippedGear.Amulet ?? DBNull.Value);
            command.Parameters.AddWithValue("ring1", (object?)player.EquippedGear.Ring1 ?? DBNull.Value);
            command.Parameters.AddWithValue("ring2", (object?)player.EquippedGear.Ring2 ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task UpdateCombatStatsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            const string sql = """
                UPDATE game.combat_stats
                SET
                    armor_class = @armorClass,
                    proficiency_bonus = @proficiencyBonus
                WHERE player_id = @playerId;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);

            command.Parameters.AddWithValue("playerId", playerId);
            command.Parameters.AddWithValue("armorClass", player.CombatStats.ArmorClass);
            command.Parameters.AddWithValue("proficiencyBonus", player.CombatStats.ProficiencyBonus);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task InsertConditionsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            foreach (var condition in player.Resources.Conditions.Other)
            {
                const string sql = """
                    INSERT INTO game.conditions
                    (
                        player_id,
                        name,
                        description,
                        duration_turns,
                        power,
                        is_permanent
                    )
                    VALUES
                    (
                        @playerId,
                        @name,
                        @description,
                        @durationTurns,
                        @power,
                        @isPermanent
                    );
                """;

                await using var command = new NpgsqlCommand(sql, connection, transaction);

                command.Parameters.AddWithValue("playerId", playerId);
                command.Parameters.AddWithValue("name", condition.Name);
                command.Parameters.AddWithValue("description", condition.Description);
                command.Parameters.AddWithValue("durationTurns", (object?)condition.DurationTurns ?? DBNull.Value);
                command.Parameters.AddWithValue("power", (object?)condition.Power ?? DBNull.Value);
                command.Parameters.AddWithValue("isPermanent", condition.IsPermanent);

                await command.ExecuteNonQueryAsync();
            }

            if (player.Resources.Conditions.Stunned)
            {
                const string stunnedSql = """
                    INSERT INTO game.conditions
                    (
                        player_id,
                        name,
                        description,
                        duration_turns,
                        power,
                        is_permanent
                    )
                    VALUES
                    (
                        @playerId,
                        'Ошеломление',
                        'Персонаж ошеломлен.',
                        NULL,
                        NULL,
                        FALSE
                    );
                """;

                await using var command = new NpgsqlCommand(stunnedSql, connection, transaction);
                command.Parameters.AddWithValue("playerId", playerId);

                await command.ExecuteNonQueryAsync();
            }
        }

        private static async Task InsertAbilitiesAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            foreach (var ability in player.Abilities.ClassAbilities)
            {
                await InsertAbilityAsync(connection, transaction, playerId, "class", ability);
            }

            foreach (var ability in player.Abilities.SpeciesAbilities)
            {
                await InsertAbilityAsync(connection, transaction, playerId, "species", ability);
            }

            foreach (var ability in player.Abilities.Feats)
            {
                await InsertAbilityAsync(connection, transaction, playerId, "feat", ability);
            }
        }

        private static async Task InsertAbilityAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            string abilityType,
            Ability ability)
        {
            const string sql = """
                INSERT INTO game.abilities
                (
                    player_id,
                    ability_type,
                    name,
                    description
                )
                VALUES
                (
                    @playerId,
                    @abilityType,
                    @name,
                    @description
                );
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);

            command.Parameters.AddWithValue("playerId", playerId);
            command.Parameters.AddWithValue("abilityType", abilityType);
            command.Parameters.AddWithValue("name", ability.Name);
            command.Parameters.AddWithValue("description", ability.Description);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task InsertInventoryAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            for (var i = 0; i < player.Inventory.Count; i++)
            {
                var item = player.Inventory[i];

                const string sql = """
                    INSERT INTO game.inventory_items
                    (
                        player_id,
                        item_order,
                        name,
                        item_type,
                        damage,
                        weight,
                        quantity,
                        armor_class,
                        armor_class_bonus
                    )
                    VALUES
                    (
                        @playerId,
                        @itemOrder,
                        @name,
                        @itemType,
                        @damage,
                        @weight,
                        @quantity,
                        @armorClass,
                        @armorClassBonus
                    );
                """;

                await using var command = new NpgsqlCommand(sql, connection, transaction);

                command.Parameters.AddWithValue("playerId", playerId);
                command.Parameters.AddWithValue("itemOrder", i);
                command.Parameters.AddWithValue("name", item.Name);
                command.Parameters.AddWithValue("itemType", (object?)item.Type ?? DBNull.Value);
                command.Parameters.AddWithValue("damage", (object?)item.Damage ?? DBNull.Value);
                command.Parameters.AddWithValue("weight", (object?)item.Weight ?? DBNull.Value);
                command.Parameters.AddWithValue("quantity", (object?)item.Quantity ?? DBNull.Value);
                command.Parameters.AddWithValue("armorClass", (object?)item.ArmorClass ?? DBNull.Value);
                command.Parameters.AddWithValue("armorClassBonus", (object?)item.ArmorClassBonus ?? DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
        }

        private static async Task InsertOtherWeaponProficienciesAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            foreach (var weaponName in player.EquipmentProficiency.Weapons.Other)
            {
                const string sql = """
                    INSERT INTO game.weapon_other_proficiencies
                    (
                        player_id,
                        weapon_name
                    )
                    VALUES
                    (
                        @playerId,
                        @weaponName
                    );
                """;

                await using var command = new NpgsqlCommand(sql, connection, transaction);

                command.Parameters.AddWithValue("playerId", playerId);
                command.Parameters.AddWithValue("weaponName", weaponName);

                await command.ExecuteNonQueryAsync();
            }
        }

        private static async Task InsertAttacksAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            for (var i = 0; i < player.CombatStats.Attacks.Count; i++)
            {
                var attack = player.CombatStats.Attacks[i];

                const string sql = """
                    INSERT INTO game.attacks
                    (
                        player_id,
                        attack_order,
                        name,
                        roll,
                        damage
                    )
                    VALUES
                    (
                        @playerId,
                        @attackOrder,
                        @name,
                        @roll,
                        @damage
                    );
                """;

                await using var command = new NpgsqlCommand(sql, connection, transaction);

                command.Parameters.AddWithValue("playerId", playerId);
                command.Parameters.AddWithValue("attackOrder", i);
                command.Parameters.AddWithValue("name", attack.Name);
                command.Parameters.AddWithValue("roll", attack.Roll);
                command.Parameters.AddWithValue("damage", attack.Damage);

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}