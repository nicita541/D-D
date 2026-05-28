using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using backend.Models;
using Npgsql;
using NpgsqlTypes;

namespace backend.Services
{
    /// <summary>
    /// Compatibility service for the old PlayersController.
    ///
    /// The database is now GameState-based. A Player is stored inside a game_state,
    /// and the readable document comes from game.game_state_documents.
    ///
    /// This service intentionally does not use the removed legacy tables/view:
    /// - game.player_documents
    /// - game.resources
    /// - game.attributes
    /// - game.inventory_items
    /// - game.equipment_proficiencies
    /// - game.movement
    ///
    /// New database tables used here:
    /// - auth.accounts
    /// - game.game_states
    /// - game.players
    /// - game.player_progression
    /// - game.player_resources
    /// - game.player_attributes
    /// - game.player_proficiencies
    /// - game.abilities
    /// - game.wealth
    /// - game.player_needs
    /// - game.item_instances
    /// - game.equipped_gear
    /// - game.combat_stats
    /// - game.attacks
    /// - game.conditions
    /// - game.limited_resources
    /// - game.game_state_documents
    /// </summary>
    public class PlayerDatabaseService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

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
                SELECT (data->'игрок')::text AS player_json
                FROM game.game_state_documents
                ORDER BY data->'игрок'->'персонаж'->>'имя';
            """;

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var json = reader.GetString(0);
                var player = JsonSerializer.Deserialize<Player>(json, JsonOptions);

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
                SELECT (d.data->'игрок')::text AS player_json
                FROM game.game_state_documents d
                JOIN game.players p ON p.game_state_id = d.game_state_id
                WHERE p.id = @id
                LIMIT 1;
            """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", id);

            var result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
            {
                return null;
            }

            var json = result.ToString();

            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<Player>(json, JsonOptions);
        }

        /// <summary>
        /// Creates a new GameState through game.create_new_game and then fills its default
        /// player with the provided Player data. This keeps old POST /api/Players usable
        /// until the real GameState/Auth API is implemented.
        /// </summary>
        public async Task<Guid> CreatePlayerAsync(Player player)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var accountId = await EnsureDevelopmentAccountAsync(connection, transaction);
                var saveName = string.IsNullOrWhiteSpace(player.Character.Name)
                    ? "Новая игра"
                    : player.Character.Name;

                var gameStateId = await CreateGameStateAsync(connection, transaction, accountId, saveName);
                var playerId = await GetPlayerIdByGameStateAsync(connection, transaction, gameStateId);

                await UpdatePlayerProfileAsync(connection, transaction, playerId, player);
                await UpdateProgressionAsync(connection, transaction, playerId, player);
                await UpdateResourcesAsync(connection, transaction, playerId, player);
                await ReplaceLimitedResourcesAsync(connection, transaction, gameStateId, playerId, player);
                await ReplaceConditionsAsync(connection, transaction, gameStateId, playerId, player);
                await UpdateAttributesAsync(connection, transaction, playerId, player);
                await ReplaceProficienciesAsync(connection, transaction, gameStateId, playerId, player);
                await ReplaceAbilitiesAsync(connection, transaction, gameStateId, playerId, player);
                await UpdateWealthAsync(connection, transaction, playerId, player);
                await UpdateNeedsAsync(connection, transaction, playerId, player);
                await UpdateCombatStatsAsync(connection, transaction, playerId, player);

                var itemMap = await ReplaceInventoryAsync(connection, transaction, gameStateId, playerId, player);
                await UpdateEquippedGearAsync(connection, transaction, playerId, player, itemMap);
                await ReplaceAttacksAsync(connection, transaction, gameStateId, playerId, player, itemMap);

                await transaction.CommitAsync();
                return playerId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Kept for old code samples/controllers that called SavePlayersAsync after mutating a Player in memory.
        /// PostgreSQL writes are now done directly in each method, so this is intentionally a no-op.
        /// </summary>
        public Task SavePlayersAsync()
        {
            return Task.CompletedTask;
        }

        private static async Task<Guid> EnsureDevelopmentAccountAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction)
        {
            const string selectSql = """
                SELECT id
                FROM auth.accounts
                WHERE email = 'dev-local@example.com'
                LIMIT 1;
            """;

            await using (var selectCommand = new NpgsqlCommand(selectSql, connection, transaction))
            {
                var existing = await selectCommand.ExecuteScalarAsync();
                if (existing is Guid existingId)
                {
                    return existingId;
                }
            }

            var accountId = Guid.NewGuid();
            var fakeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"dev-local:{accountId}"))).ToLowerInvariant();

            const string insertSql = """
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
                );
            """;

            await using var insertCommand = new NpgsqlCommand(insertSql, connection, transaction);
            insertCommand.Parameters.AddWithValue("id", accountId);
            insertCommand.Parameters.AddWithValue("passwordHash", fakeHash);
            await insertCommand.ExecuteNonQueryAsync();

            return accountId;
        }

        private static async Task<Guid> CreateGameStateAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid accountId,
            string saveName)
        {
            const string sql = """
                SELECT game.create_new_game(@accountId, @saveName);
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("accountId", accountId);
            command.Parameters.AddWithValue("saveName", saveName);

            return (Guid)(await command.ExecuteScalarAsync()
                ?? throw new InvalidOperationException("game.create_new_game did not return a game_state id."));
        }

        private static async Task<Guid> GetPlayerIdByGameStateAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid gameStateId)
        {
            const string sql = """
                SELECT id
                FROM game.players
                WHERE game_state_id = @gameStateId
                LIMIT 1;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("gameStateId", gameStateId);

            return (Guid)(await command.ExecuteScalarAsync()
                ?? throw new InvalidOperationException("New game_state does not contain a player."));
        }

        private static async Task UpdatePlayerProfileAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            const string sql = """
                UPDATE game.players
                SET
                    name = @name,
                    background = @background,
                    species = @species,
                    class_name = @className,
                    subclass = @subclass,
                    description = @description,
                    alignment = @alignment
                WHERE id = @playerId;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("playerId", playerId);
            command.Parameters.AddWithValue("name", NullIfEmpty(player.Character.Name) ?? "Безымянный");
            command.Parameters.AddWithValue("background", ToDb(player.Character.Background));
            command.Parameters.AddWithValue("species", ToDb(player.Character.Species));
            command.Parameters.AddWithValue("className", ToDb(player.Character.Class));
            command.Parameters.AddWithValue("subclass", ToDb(player.Character.Subclass));
            command.Parameters.AddWithValue("description", ToDb(player.Character.Description));
            command.Parameters.AddWithValue("alignment", ToDb(player.Character.Alignment));

            await command.ExecuteNonQueryAsync();
        }

        private static async Task UpdateProgressionAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            const string sql = """
                INSERT INTO game.player_progression
                (
                    player_id,
                    level,
                    experience,
                    experience_to_next_level
                )
                VALUES
                (
                    @playerId,
                    @level,
                    @experience,
                    @experienceToNextLevel
                )
                ON CONFLICT (player_id)
                DO UPDATE SET
                    level = EXCLUDED.level,
                    experience = EXCLUDED.experience,
                    experience_to_next_level = EXCLUDED.experience_to_next_level;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("playerId", playerId);
            command.Parameters.AddWithValue("level", player.Progression.Level);
            command.Parameters.AddWithValue("experience", player.Progression.Experience);
            command.Parameters.AddWithValue("experienceToNextLevel", player.Progression.ExperienceToNextLevel);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task UpdateResourcesAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            const string sql = """
                INSERT INTO game.player_resources
                (
                    player_id,
                    hp_max,
                    hp_current,
                    mana_max,
                    mana_current,
                    action_points_max,
                    action_points_current,
                    death_saves_active,
                    death_saves_successes,
                    death_saves_failures
                )
                VALUES
                (
                    @playerId,
                    @hpMax,
                    @hpCurrent,
                    @manaMax,
                    @manaCurrent,
                    @actionPointsMax,
                    @actionPointsCurrent,
                    @deathSavesActive,
                    @deathSavesSuccesses,
                    @deathSavesFailures
                )
                ON CONFLICT (player_id)
                DO UPDATE SET
                    hp_max = EXCLUDED.hp_max,
                    hp_current = EXCLUDED.hp_current,
                    mana_max = EXCLUDED.mana_max,
                    mana_current = EXCLUDED.mana_current,
                    action_points_max = EXCLUDED.action_points_max,
                    action_points_current = EXCLUDED.action_points_current,
                    death_saves_active = EXCLUDED.death_saves_active,
                    death_saves_successes = EXCLUDED.death_saves_successes,
                    death_saves_failures = EXCLUDED.death_saves_failures;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("playerId", playerId);
            command.Parameters.AddWithValue("hpMax", player.Resources.Hp.Max);
            command.Parameters.AddWithValue("hpCurrent", player.Resources.Hp.Current);
            command.Parameters.AddWithValue("manaMax", player.Resources.Mana.Max);
            command.Parameters.AddWithValue("manaCurrent", player.Resources.Mana.Current);
            command.Parameters.AddWithValue("actionPointsMax", player.Resources.ActionPoints.Max);
            command.Parameters.AddWithValue("actionPointsCurrent", player.Resources.ActionPoints.Current);
            command.Parameters.AddWithValue("deathSavesActive", player.Resources.DeathSaves.IsActive);
            command.Parameters.AddWithValue("deathSavesSuccesses", player.Resources.DeathSaves.Successes);
            command.Parameters.AddWithValue("deathSavesFailures", player.Resources.DeathSaves.Failures);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task ReplaceLimitedResourcesAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid gameStateId,
            Guid playerId,
            Player player)
        {
            await DeleteByPlayerAsync(connection, transaction, "game.limited_resources", playerId);

            foreach (var resource in player.Resources.LimitedResources)
            {
                var resourceId = ParseGuidOrNew(resource.Id);

                const string sql = """
                    INSERT INTO game.limited_resources
                    (
                        id,
                        game_state_id,
                        player_id,
                        name,
                        max_value,
                        current_value,
                        recovery
                    )
                    VALUES
                    (
                        @id,
                        @gameStateId,
                        @playerId,
                        @name,
                        @maxValue,
                        @currentValue,
                        @recovery
                    );
                """;

                await using var command = new NpgsqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("id", resourceId);
                command.Parameters.AddWithValue("gameStateId", gameStateId);
                command.Parameters.AddWithValue("playerId", playerId);
                command.Parameters.AddWithValue("name", resource.Name);
                command.Parameters.AddWithValue("maxValue", resource.Max);
                command.Parameters.AddWithValue("currentValue", resource.Current);
                command.Parameters.AddWithValue("recovery", resource.Recovery);

                await command.ExecuteNonQueryAsync();
            }
        }

        private static async Task ReplaceConditionsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid gameStateId,
            Guid playerId,
            Player player)
        {
            await DeleteByPlayerAsync(connection, transaction, "game.conditions", playerId);

            foreach (var condition in player.Resources.Conditions)
            {
                var conditionId = ParseGuidOrNew(condition.Id);

                const string sql = """
                    INSERT INTO game.conditions
                    (
                        id,
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
                        @id,
                        @gameStateId,
                        @playerId,
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
                    );
                """;

                await using var command = new NpgsqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("id", conditionId);
                command.Parameters.AddWithValue("gameStateId", gameStateId);
                command.Parameters.AddWithValue("playerId", playerId);
                command.Parameters.AddWithValue("name", condition.Name);
                command.Parameters.AddWithValue("type", NullIfEmpty(condition.Type) ?? "effect");
                command.Parameters.AddWithValue("description", ToDb(condition.Description));
                command.Parameters.AddWithValue("source", ToDb(condition.Source));
                command.Parameters.AddWithValue("remainingTurns", ToDb(condition.RemainingTurns));
                command.Parameters.AddWithValue("isPermanent", condition.IsPermanent);
                command.Parameters.AddWithValue("stacks", condition.Stacks <= 0 ? 1 : condition.Stacks);
                command.Parameters.AddWithValue("maxStacks", ToDb(condition.MaxStacks));
                command.Parameters.AddWithValue("effects", JsonSerializer.Serialize(condition.Effects, JsonOptions));
                command.Parameters.AddWithValue("tags", JsonSerializer.Serialize(condition.Tags, JsonOptions));

                await command.ExecuteNonQueryAsync();
            }
        }

        private static async Task UpdateAttributesAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            const string sql = """
                INSERT INTO game.player_attributes
                (
                    player_id,
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
                    @playerId,
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

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("playerId", playerId);
            command.Parameters.AddWithValue("strength", player.Attributes.Strength);
            command.Parameters.AddWithValue("dexterity", player.Attributes.Dexterity);
            command.Parameters.AddWithValue("constitution", player.Attributes.Constitution);
            command.Parameters.AddWithValue("intelligence", player.Attributes.Intelligence);
            command.Parameters.AddWithValue("wisdom", player.Attributes.Wisdom);
            command.Parameters.AddWithValue("charisma", player.Attributes.Charisma);
            command.Parameters.AddWithValue("initiative", player.Attributes.Initiative);
            command.Parameters.AddWithValue("speed", player.Attributes.Speed);
            command.Parameters.AddWithValue("perception", player.Attributes.Perception);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task ReplaceProficienciesAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid gameStateId,
            Guid playerId,
            Player player)
        {
            await DeleteByPlayerAsync(connection, transaction, "game.player_proficiencies", playerId);

            foreach (var value in player.Proficiencies.Armor)
            {
                await InsertProficiencyAsync(connection, transaction, gameStateId, playerId, "armor", value);
            }

            foreach (var value in player.Proficiencies.Weapons)
            {
                await InsertProficiencyAsync(connection, transaction, gameStateId, playerId, "weapon", value);
            }

            foreach (var value in player.Proficiencies.Skills)
            {
                await InsertProficiencyAsync(connection, transaction, gameStateId, playerId, "skill", value);
            }

            foreach (var value in player.Proficiencies.SavingThrows)
            {
                await InsertProficiencyAsync(connection, transaction, gameStateId, playerId, "saving_throw", value);
            }

            foreach (var value in player.Proficiencies.Tools)
            {
                await InsertProficiencyAsync(connection, transaction, gameStateId, playerId, "tool", value);
            }

            foreach (var value in player.Proficiencies.Languages)
            {
                await InsertProficiencyAsync(connection, transaction, gameStateId, playerId, "language", value);
            }
        }

        private static async Task InsertProficiencyAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid gameStateId,
            Guid playerId,
            string type,
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            const string sql = """
                INSERT INTO game.player_proficiencies
                (
                    id,
                    game_state_id,
                    player_id,
                    type,
                    value
                )
                VALUES
                (
                    gen_random_uuid(),
                    @gameStateId,
                    @playerId,
                    @type,
                    @value
                )
                ON CONFLICT (player_id, type, value) DO NOTHING;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("gameStateId", gameStateId);
            command.Parameters.AddWithValue("playerId", playerId);
            command.Parameters.AddWithValue("type", type);
            command.Parameters.AddWithValue("value", value);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task ReplaceAbilitiesAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid gameStateId,
            Guid playerId,
            Player player)
        {
            await DeleteByPlayerAsync(connection, transaction, "game.abilities", playerId);

            foreach (var ability in player.Abilities.ClassAbilities)
            {
                await InsertAbilityAsync(connection, transaction, gameStateId, playerId, "class", ability);
            }

            foreach (var ability in player.Abilities.SpeciesAbilities)
            {
                await InsertAbilityAsync(connection, transaction, gameStateId, playerId, "species", ability);
            }

            foreach (var ability in player.Abilities.Feats)
            {
                await InsertAbilityAsync(connection, transaction, gameStateId, playerId, "feat", ability);
            }

            foreach (var ability in player.Abilities.Spells)
            {
                await InsertAbilityAsync(connection, transaction, gameStateId, playerId, "spell", ability);
            }
        }

        private static async Task InsertAbilityAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid gameStateId,
            Guid playerId,
            string category,
            Ability ability)
        {
            var abilityId = ParseGuidOrNew(ability.Id);
            var costResourceId = ParseNullableGuid(ability.Cost?.ResourceId);

            const string sql = """
                INSERT INTO game.abilities
                (
                    id,
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
                    @id,
                    @gameStateId,
                    @playerId,
                    @category,
                    @name,
                    @description,
                    @abilityType,
                    @costResourceId,
                    @costAmount,
                    @effects::jsonb
                );
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("id", abilityId);
            command.Parameters.AddWithValue("gameStateId", gameStateId);
            command.Parameters.AddWithValue("playerId", playerId);
            command.Parameters.AddWithValue("category", category);
            command.Parameters.AddWithValue("name", ability.Name);
            command.Parameters.AddWithValue("description", ToDb(ability.Description));
            command.Parameters.AddWithValue("abilityType", NullIfEmpty(ability.Type) ?? "passive");
            AddUuidParameter(command, "costResourceId", costResourceId);
            command.Parameters.AddWithValue("costAmount", ToDb(ability.Cost?.Amount));
            command.Parameters.AddWithValue("effects", JsonSerializer.Serialize(ability.Effects, JsonOptions));

            await command.ExecuteNonQueryAsync();
        }

        private static async Task UpdateWealthAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            const string sql = """
                INSERT INTO game.wealth
                (
                    player_id,
                    copper,
                    silver,
                    gold,
                    platinum
                )
                VALUES
                (
                    @playerId,
                    @copper,
                    @silver,
                    @gold,
                    @platinum
                )
                ON CONFLICT (player_id)
                DO UPDATE SET
                    copper = EXCLUDED.copper,
                    silver = EXCLUDED.silver,
                    gold = EXCLUDED.gold,
                    platinum = EXCLUDED.platinum;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("playerId", playerId);
            command.Parameters.AddWithValue("copper", player.Wealth.Coins.Copper);
            command.Parameters.AddWithValue("silver", player.Wealth.Coins.Silver);
            command.Parameters.AddWithValue("gold", player.Wealth.Coins.Gold);
            command.Parameters.AddWithValue("platinum", player.Wealth.Coins.Platinum);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task UpdateNeedsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            const string sql = """
                INSERT INTO game.player_needs
                (
                    player_id,
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
                    @playerId,
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

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("playerId", playerId);
            command.Parameters.AddWithValue("foodSize", player.Needs.Food.Size);
            command.Parameters.AddWithValue("foodPerDay", ToDb(player.Needs.Food.PerDay));
            command.Parameters.AddWithValue("foodRemaining", player.Needs.Food.Remaining);
            command.Parameters.AddWithValue("waterSize", player.Needs.Water.Size);
            command.Parameters.AddWithValue("waterPerDay", ToDb(player.Needs.Water.PerDay));
            command.Parameters.AddWithValue("waterRemaining", player.Needs.Water.Remaining);
            command.Parameters.AddWithValue("carryCapacityMax", player.Needs.CarryingCapacity.Max);
            command.Parameters.AddWithValue("carryCurrentWeight", player.Needs.CarryingCapacity.CurrentWeight);
            command.Parameters.AddWithValue("carryUnit", player.Needs.CarryingCapacity.Unit);
            command.Parameters.AddWithValue("movementMetersPerTurn", player.Needs.Movement.MetersPerTurn);
            command.Parameters.AddWithValue("movementKmPerDay", player.Needs.Movement.KmPerDay);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task UpdateCombatStatsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player)
        {
            const string sql = """
                INSERT INTO game.combat_stats
                (
                    player_id,
                    armor_class,
                    proficiency_bonus,
                    in_combat,
                    initiative_roll
                )
                VALUES
                (
                    @playerId,
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

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("playerId", playerId);
            command.Parameters.AddWithValue("armorClass", player.Combat.ArmorClass);
            command.Parameters.AddWithValue("proficiencyBonus", player.Combat.ProficiencyBonus);
            command.Parameters.AddWithValue("inCombat", player.Combat.InCombat);
            command.Parameters.AddWithValue("initiativeRoll", player.Combat.InitiativeRoll);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task<Dictionary<string, Guid>> ReplaceInventoryAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid gameStateId,
            Guid playerId,
            Player player)
        {
            await using (var deleteConsumables = new NpgsqlCommand(
                """
                DELETE FROM game.item_consumable_stats
                WHERE item_id IN
                (
                    SELECT id
                    FROM game.item_instances
                    WHERE game_state_id = @gameStateId
                      AND owner_kind = 'player_inventory'
                      AND owner_id = @playerId
                );
                """,
                connection,
                transaction))
            {
                deleteConsumables.Parameters.AddWithValue("gameStateId", gameStateId);
                deleteConsumables.Parameters.AddWithValue("playerId", playerId);
                await deleteConsumables.ExecuteNonQueryAsync();
            }

            await using (var deleteArmor = new NpgsqlCommand(
                """
                DELETE FROM game.item_armor_stats
                WHERE item_id IN
                (
                    SELECT id
                    FROM game.item_instances
                    WHERE game_state_id = @gameStateId
                      AND owner_kind = 'player_inventory'
                      AND owner_id = @playerId
                );
                """,
                connection,
                transaction))
            {
                deleteArmor.Parameters.AddWithValue("gameStateId", gameStateId);
                deleteArmor.Parameters.AddWithValue("playerId", playerId);
                await deleteArmor.ExecuteNonQueryAsync();
            }

            await using (var deleteWeapon = new NpgsqlCommand(
                """
                DELETE FROM game.item_weapon_stats
                WHERE item_id IN
                (
                    SELECT id
                    FROM game.item_instances
                    WHERE game_state_id = @gameStateId
                      AND owner_kind = 'player_inventory'
                      AND owner_id = @playerId
                );
                """,
                connection,
                transaction))
            {
                deleteWeapon.Parameters.AddWithValue("gameStateId", gameStateId);
                deleteWeapon.Parameters.AddWithValue("playerId", playerId);
                await deleteWeapon.ExecuteNonQueryAsync();
            }

            await using (var deleteItems = new NpgsqlCommand(
                """
                DELETE FROM game.item_instances
                WHERE game_state_id = @gameStateId
                  AND owner_kind = 'player_inventory'
                  AND owner_id = @playerId;
                """,
                connection,
                transaction))
            {
                deleteItems.Parameters.AddWithValue("gameStateId", gameStateId);
                deleteItems.Parameters.AddWithValue("playerId", playerId);
                await deleteItems.ExecuteNonQueryAsync();
            }

            var itemMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in player.Inventory)
            {
                var itemId = ParseGuidOrNew(item.Id);

                if (!string.IsNullOrWhiteSpace(item.Id))
                {
                    itemMap[item.Id] = itemId;
                }

                if (!string.IsNullOrWhiteSpace(item.Name))
                {
                    itemMap[item.Name] = itemId;
                }

                const string insertItemSql = """
                    INSERT INTO game.item_instances
                    (
                        id,
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
                        @id,
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
                        @playerId
                    );
                """;

                await using (var command = new NpgsqlCommand(insertItemSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("id", itemId);
                    command.Parameters.AddWithValue("gameStateId", gameStateId);
                    command.Parameters.AddWithValue("templateId", ToDb(item.TemplateId));
                    command.Parameters.AddWithValue("name", item.Name);
                    command.Parameters.AddWithValue("itemType", NullIfEmpty(item.Type) ?? "item");
                    command.Parameters.AddWithValue("subtype", ToDb(item.Subtype));
                    command.Parameters.AddWithValue("description", ToDb(item.Description));
                    command.Parameters.AddWithValue("quantity", item.Quantity <= 0 ? 1 : item.Quantity);
                    command.Parameters.AddWithValue("stackable", item.Stackable);
                    command.Parameters.AddWithValue("weightEach", item.WeightEach);
                    command.Parameters.AddWithValue("condition", NullIfEmpty(item.Condition) ?? "normal");
                    command.Parameters.AddWithValue("rarity", NullIfEmpty(item.Rarity) ?? "common");
                    command.Parameters.AddWithValue("isMagical", item.IsMagical);
                    command.Parameters.AddWithValue("priceCopper", item.Price?.Copper ?? 0);
                    command.Parameters.AddWithValue("priceSilver", item.Price?.Silver ?? 0);
                    command.Parameters.AddWithValue("priceGold", item.Price?.Gold ?? 0);
                    command.Parameters.AddWithValue("pricePlatinum", item.Price?.Platinum ?? 0);
                    command.Parameters.AddWithValue("tags", JsonSerializer.Serialize(item.Tags, JsonOptions));
                    command.Parameters.AddWithValue("playerId", playerId);

                    await command.ExecuteNonQueryAsync();
                }

                if (item.Weapon != null)
                {
                    await InsertWeaponStatsAsync(connection, transaction, itemId, item.Weapon);
                }

                if (item.Armor != null)
                {
                    await InsertArmorStatsAsync(connection, transaction, itemId, item.Armor);
                }

                if (item.Consumable != null)
                {
                    await InsertConsumableStatsAsync(connection, transaction, itemId, item.Consumable);
                }
            }

            return itemMap;
        }

        private static async Task InsertWeaponStatsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid itemId,
            WeaponStats weapon)
        {
            const string sql = """
                INSERT INTO game.item_weapon_stats
                (
                    item_id,
                    damage_dice,
                    damage_type,
                    attack_bonus,
                    damage_bonus,
                    range,
                    properties
                )
                VALUES
                (
                    @itemId,
                    @damageDice,
                    @damageType,
                    @attackBonus,
                    @damageBonus,
                    @range,
                    @properties::jsonb
                );
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("itemId", itemId);
            command.Parameters.AddWithValue("damageDice", weapon.DamageDice);
            command.Parameters.AddWithValue("damageType", weapon.DamageType);
            command.Parameters.AddWithValue("attackBonus", weapon.AttackBonus);
            command.Parameters.AddWithValue("damageBonus", weapon.DamageBonus);
            command.Parameters.AddWithValue("range", ToDb(weapon.Range));
            command.Parameters.AddWithValue("properties", JsonSerializer.Serialize(weapon.Properties, JsonOptions));

            await command.ExecuteNonQueryAsync();
        }

        private static async Task InsertArmorStatsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid itemId,
            ArmorStats armor)
        {
            const string sql = """
                INSERT INTO game.item_armor_stats
                (
                    item_id,
                    armor_class,
                    armor_class_bonus,
                    armor_type,
                    stealth_disadvantage,
                    strength_requirement
                )
                VALUES
                (
                    @itemId,
                    @armorClass,
                    @armorClassBonus,
                    @armorType,
                    @stealthDisadvantage,
                    @strengthRequirement
                );
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("itemId", itemId);
            command.Parameters.AddWithValue("armorClass", armor.ArmorClass);
            command.Parameters.AddWithValue("armorClassBonus", armor.ArmorClassBonus);
            command.Parameters.AddWithValue("armorType", ToDb(armor.ArmorType));
            command.Parameters.AddWithValue("stealthDisadvantage", armor.StealthDisadvantage);
            command.Parameters.AddWithValue("strengthRequirement", ToDb(armor.StrengthRequirement));

            await command.ExecuteNonQueryAsync();
        }

        private static async Task InsertConsumableStatsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid itemId,
            ConsumableStats consumable)
        {
            const string sql = """
                INSERT INTO game.item_consumable_stats
                (
                    item_id,
                    uses,
                    effects
                )
                VALUES
                (
                    @itemId,
                    @uses,
                    @effects::jsonb
                );
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("itemId", itemId);
            command.Parameters.AddWithValue("uses", consumable.Uses);
            command.Parameters.AddWithValue("effects", JsonSerializer.Serialize(consumable.Effects, JsonOptions));

            await command.ExecuteNonQueryAsync();
        }

        private static async Task UpdateEquippedGearAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid playerId,
            Player player,
            Dictionary<string, Guid> itemMap)
        {
            const string sql = """
                INSERT INTO game.equipped_gear
                (
                    player_id,
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
                    @playerId,
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

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("playerId", playerId);
            AddUuidParameter(command, "headItemId", ResolveItemId(player.Equipment.HeadItemId, itemMap));
            AddUuidParameter(command, "bodyItemId", ResolveItemId(player.Equipment.BodyItemId, itemMap));
            AddUuidParameter(command, "handsItemId", ResolveItemId(player.Equipment.HandsItemId, itemMap));
            AddUuidParameter(command, "legsItemId", ResolveItemId(player.Equipment.LegsItemId, itemMap));
            AddUuidParameter(command, "feetItemId", ResolveItemId(player.Equipment.FeetItemId, itemMap));
            AddUuidParameter(command, "mainHandItemId", ResolveItemId(player.Equipment.MainHandItemId, itemMap));
            AddUuidParameter(command, "offHandItemId", ResolveItemId(player.Equipment.OffHandItemId, itemMap));
            AddUuidParameter(command, "amuletItemId", ResolveItemId(player.Equipment.AmuletItemId, itemMap));
            AddUuidParameter(command, "ring1ItemId", ResolveItemId(player.Equipment.Ring1ItemId, itemMap));
            AddUuidParameter(command, "ring2ItemId", ResolveItemId(player.Equipment.Ring2ItemId, itemMap));

            await command.ExecuteNonQueryAsync();
        }

        private static async Task ReplaceAttacksAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid gameStateId,
            Guid playerId,
            Player player,
            Dictionary<string, Guid> itemMap)
        {
            await DeleteByPlayerAsync(connection, transaction, "game.attacks", playerId);

            foreach (var attack in player.Combat.Attacks)
            {
                var attackId = ParseGuidOrNew(attack.Id);
                var itemId = ResolveItemId(attack.ItemId, itemMap);

                const string sql = """
                    INSERT INTO game.attacks
                    (
                        id,
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
                        @id,
                        @gameStateId,
                        @playerId,
                        @itemId,
                        @name,
                        @roll,
                        @damage,
                        @damageType
                    );
                """;

                await using var command = new NpgsqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("id", attackId);
                command.Parameters.AddWithValue("gameStateId", gameStateId);
                command.Parameters.AddWithValue("playerId", playerId);
                AddUuidParameter(command, "itemId", itemId);
                command.Parameters.AddWithValue("name", attack.Name);
                command.Parameters.AddWithValue("roll", attack.Roll);
                command.Parameters.AddWithValue("damage", attack.Damage);
                command.Parameters.AddWithValue("damageType", ToDb(attack.DamageType));

                await command.ExecuteNonQueryAsync();
            }
        }

        private static async Task DeleteByPlayerAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            string tableName,
            Guid playerId)
        {
            var sql = $"DELETE FROM {tableName} WHERE player_id = @playerId;";

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("playerId", playerId);
            await command.ExecuteNonQueryAsync();
        }

        private static Guid ParseGuidOrNew(string? value)
        {
            return Guid.TryParse(value, out var parsed)
                ? parsed
                : Guid.NewGuid();
        }

        private static Guid? ParseNullableGuid(string? value)
        {
            return Guid.TryParse(value, out var parsed)
                ? parsed
                : null;
        }

        private static Guid? ResolveItemId(string? value, IReadOnlyDictionary<string, Guid> itemMap)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (Guid.TryParse(value, out var parsed))
            {
                return parsed;
            }

            return itemMap.TryGetValue(value, out var mapped)
                ? mapped
                : null;
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static object ToDb(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
        }

        private static object ToDb(int? value)
        {
            return value.HasValue ? value.Value : DBNull.Value;
        }

        private static void AddUuidParameter(NpgsqlCommand command, string name, Guid? value)
        {
            var parameter = command.Parameters.Add(name, NpgsqlDbType.Uuid);
            parameter.Value = value.HasValue ? value.Value : DBNull.Value;
        }
    }
}
