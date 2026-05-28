CREATE EXTENSION IF NOT EXISTS pgcrypto;

DROP SCHEMA IF EXISTS game CASCADE;
DROP SCHEMA IF EXISTS auth CASCADE;

CREATE SCHEMA auth;
CREATE SCHEMA game;

CREATE OR REPLACE FUNCTION game.set_updated_at()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$;

CREATE TABLE auth.accounts (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email text NOT NULL UNIQUE,
    username text NOT NULL UNIQUE,
    password_hash text NOT NULL,
    display_name text,
    role text NOT NULL DEFAULT 'user',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    last_login_at timestamptz,
    is_active boolean NOT NULL DEFAULT true,
    CONSTRAINT accounts_email_not_empty CHECK (btrim(email) <> ''),
    CONSTRAINT accounts_username_not_empty CHECK (btrim(username) <> ''),
    CONSTRAINT accounts_password_hash_not_empty CHECK (btrim(password_hash) <> ''),
    CONSTRAINT accounts_role_check CHECK (role IN ('user', 'admin'))
);

CREATE UNIQUE INDEX ux_accounts_email_lower ON auth.accounts (lower(email));
CREATE UNIQUE INDEX ux_accounts_username_lower ON auth.accounts (lower(username));

CREATE TRIGGER accounts_set_updated_at
BEFORE UPDATE ON auth.accounts
FOR EACH ROW
EXECUTE FUNCTION game.set_updated_at();

CREATE TABLE auth.refresh_tokens (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES auth.accounts(id) ON DELETE CASCADE,
    token_hash text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    expires_at timestamptz NOT NULL,
    revoked_at timestamptz,
    created_by_ip text,
    revoked_by_ip text,
    user_agent text,
    replaced_by_token_hash text,
    CONSTRAINT refresh_tokens_token_hash_not_empty CHECK (btrim(token_hash) <> '')
);

CREATE INDEX refresh_tokens_account_id_idx ON auth.refresh_tokens(account_id);
CREATE INDEX refresh_tokens_expires_at_idx ON auth.refresh_tokens(expires_at);
CREATE UNIQUE INDEX ux_refresh_tokens_token_hash ON auth.refresh_tokens(token_hash);
CREATE INDEX ix_refresh_tokens_account_id ON auth.refresh_tokens(account_id);
CREATE INDEX ix_refresh_tokens_expires_at ON auth.refresh_tokens(expires_at);

CREATE TABLE game.game_states (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES auth.accounts(id) ON DELETE CASCADE,
    name text NOT NULL DEFAULT 'Новая игра',
    schema_version text NOT NULL DEFAULT '1.0',
    turn_number integer NOT NULL DEFAULT 0 CHECK (turn_number >= 0),
    mode text NOT NULL DEFAULT 'exploration',
    current_location_id uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    is_active boolean NOT NULL DEFAULT true,
    CONSTRAINT game_states_mode_check CHECK (mode IN ('exploration', 'combat', 'dialogue', 'travel', 'rest')),
    CONSTRAINT game_states_id_account_id_unique UNIQUE (id, account_id)
);

CREATE INDEX game_states_account_id_idx ON game.game_states(account_id);
CREATE INDEX game_states_account_id_is_active_idx ON game.game_states(account_id, is_active);
CREATE INDEX game_states_updated_at_idx ON game.game_states(updated_at);

CREATE TRIGGER game_states_set_updated_at
BEFORE UPDATE ON game.game_states
FOR EACH ROW
EXECUTE FUNCTION game.set_updated_at();

COMMENT ON TABLE game.game_states IS 'Root save-game record. Every game table is scoped to one game_state_id so saves are isolated from each other.';
COMMENT ON COLUMN game.game_states.account_id IS 'Owner account for this save. API queries must always filter by this account.';
COMMENT ON COLUMN game.game_states.current_location_id IS 'Current location in this save. The foreign key is added after game.locations is created.';
COMMENT ON COLUMN game.game_states.mode IS 'Current gameplay mode: exploration, combat, dialogue, travel, or rest.';

CREATE TABLE game.players (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL,
    account_id uuid NOT NULL,
    name text NOT NULL DEFAULT 'Герой',
    background text,
    species text,
    class_name text,
    subclass text,
    description text,
    alignment text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT players_game_state_account_fkey FOREIGN KEY (game_state_id, account_id)
        REFERENCES game.game_states(id, account_id) ON DELETE CASCADE,
    CONSTRAINT players_id_game_state_id_unique UNIQUE (id, game_state_id)
);

CREATE INDEX players_game_state_id_idx ON game.players(game_state_id);
CREATE INDEX players_account_id_idx ON game.players(account_id);

CREATE TRIGGER players_set_updated_at
BEFORE UPDATE ON game.players
FOR EACH ROW
EXECUTE FUNCTION game.set_updated_at();

CREATE TABLE game.player_progression (
    player_id uuid PRIMARY KEY,
    game_state_id uuid NOT NULL,
    level integer NOT NULL DEFAULT 1 CHECK (level >= 1),
    experience integer NOT NULL DEFAULT 0 CHECK (experience >= 0),
    experience_to_next_level integer NOT NULL DEFAULT 300 CHECK (experience_to_next_level >= 0),
    CONSTRAINT player_progression_player_fkey FOREIGN KEY (player_id, game_state_id)
        REFERENCES game.players(id, game_state_id) ON DELETE CASCADE
);

CREATE INDEX player_progression_game_state_id_idx ON game.player_progression(game_state_id);

CREATE TABLE game.player_resources (
    player_id uuid PRIMARY KEY,
    game_state_id uuid NOT NULL,
    hp_max integer NOT NULL DEFAULT 1 CHECK (hp_max >= 0),
    hp_current integer NOT NULL DEFAULT 1 CHECK (hp_current >= 0),
    mana_max integer NOT NULL DEFAULT 0 CHECK (mana_max >= 0),
    mana_current integer NOT NULL DEFAULT 0 CHECK (mana_current >= 0),
    action_points_max integer NOT NULL DEFAULT 1 CHECK (action_points_max >= 0),
    action_points_current integer NOT NULL DEFAULT 1 CHECK (action_points_current >= 0),
    death_saves_active boolean NOT NULL DEFAULT false,
    death_saves_successes integer NOT NULL DEFAULT 0 CHECK (death_saves_successes >= 0),
    death_saves_failures integer NOT NULL DEFAULT 0 CHECK (death_saves_failures >= 0),
    CONSTRAINT player_resources_player_fkey FOREIGN KEY (player_id, game_state_id)
        REFERENCES game.players(id, game_state_id) ON DELETE CASCADE
);

CREATE INDEX player_resources_game_state_id_idx ON game.player_resources(game_state_id);

CREATE TABLE game.conditions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL,
    player_id uuid NOT NULL,
    name text NOT NULL,
    type text NOT NULL,
    description text,
    source text,
    remaining_turns integer,
    is_permanent boolean NOT NULL DEFAULT false,
    stacks integer NOT NULL DEFAULT 1 CHECK (stacks >= 1),
    max_stacks integer CHECK (max_stacks IS NULL OR max_stacks >= 1),
    effects jsonb NOT NULL DEFAULT '[]'::jsonb,
    tags jsonb NOT NULL DEFAULT '[]'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT conditions_player_fkey FOREIGN KEY (player_id, game_state_id)
        REFERENCES game.players(id, game_state_id) ON DELETE CASCADE
);

CREATE INDEX conditions_game_state_id_idx ON game.conditions(game_state_id);
CREATE INDEX conditions_player_id_idx ON game.conditions(player_id);
CREATE INDEX conditions_type_idx ON game.conditions(type);

COMMENT ON TABLE game.conditions IS 'Universal player condition table. Conditions such as poison, disease, injury, buffs, debuffs, control, and magic effects are rows, not boolean columns.';
COMMENT ON COLUMN game.conditions.type IS 'Condition category such as poison, disease, injury, buff, debuff, control, or magic.';
COMMENT ON COLUMN game.conditions.effects IS 'Flexible JSON effects interpreted by game rules or backend validation.';
COMMENT ON COLUMN game.conditions.tags IS 'Flexible JSON tags for filtering and AI/game-rule context.';

CREATE TABLE game.limited_resources (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL,
    player_id uuid NOT NULL,
    name text NOT NULL,
    max_value integer NOT NULL CHECK (max_value >= 0),
    current_value integer NOT NULL CHECK (current_value >= 0),
    recovery text NOT NULL,
    CONSTRAINT limited_resources_player_fkey FOREIGN KEY (player_id, game_state_id)
        REFERENCES game.players(id, game_state_id) ON DELETE CASCADE,
    CONSTRAINT limited_resources_player_name_unique UNIQUE (player_id, name)
);

CREATE INDEX limited_resources_game_state_id_idx ON game.limited_resources(game_state_id);
CREATE INDEX limited_resources_player_id_idx ON game.limited_resources(player_id);

CREATE TABLE game.player_attributes (
    player_id uuid PRIMARY KEY,
    game_state_id uuid NOT NULL,
    strength integer NOT NULL DEFAULT 10,
    dexterity integer NOT NULL DEFAULT 10,
    constitution integer NOT NULL DEFAULT 10,
    intelligence integer NOT NULL DEFAULT 10,
    wisdom integer NOT NULL DEFAULT 10,
    charisma integer NOT NULL DEFAULT 10,
    initiative integer NOT NULL DEFAULT 0,
    speed integer NOT NULL DEFAULT 9,
    perception integer NOT NULL DEFAULT 10,
    CONSTRAINT player_attributes_player_fkey FOREIGN KEY (player_id, game_state_id)
        REFERENCES game.players(id, game_state_id) ON DELETE CASCADE
);

CREATE INDEX player_attributes_game_state_id_idx ON game.player_attributes(game_state_id);

CREATE TABLE game.player_proficiencies (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL,
    player_id uuid NOT NULL,
    type text NOT NULL,
    value text NOT NULL,
    CONSTRAINT player_proficiencies_player_fkey FOREIGN KEY (player_id, game_state_id)
        REFERENCES game.players(id, game_state_id) ON DELETE CASCADE,
    CONSTRAINT player_proficiencies_unique UNIQUE (player_id, type, value)
);

CREATE INDEX player_proficiencies_game_state_id_idx ON game.player_proficiencies(game_state_id);
CREATE INDEX player_proficiencies_player_id_idx ON game.player_proficiencies(player_id);

CREATE TABLE game.abilities (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL,
    player_id uuid NOT NULL,
    category text NOT NULL,
    name text NOT NULL,
    description text,
    ability_type text NOT NULL,
    cost_resource_id uuid REFERENCES game.limited_resources(id) ON DELETE SET NULL,
    cost_amount integer CHECK (cost_amount IS NULL OR cost_amount >= 0),
    effects jsonb NOT NULL DEFAULT '[]'::jsonb,
    CONSTRAINT abilities_player_fkey FOREIGN KEY (player_id, game_state_id)
        REFERENCES game.players(id, game_state_id) ON DELETE CASCADE
);

CREATE INDEX abilities_game_state_id_idx ON game.abilities(game_state_id);
CREATE INDEX abilities_player_category_idx ON game.abilities(player_id, category);

CREATE TABLE game.wealth (
    player_id uuid PRIMARY KEY,
    game_state_id uuid NOT NULL,
    copper integer NOT NULL DEFAULT 0 CHECK (copper >= 0),
    silver integer NOT NULL DEFAULT 0 CHECK (silver >= 0),
    gold integer NOT NULL DEFAULT 0 CHECK (gold >= 0),
    platinum integer NOT NULL DEFAULT 0 CHECK (platinum >= 0),
    CONSTRAINT wealth_player_fkey FOREIGN KEY (player_id, game_state_id)
        REFERENCES game.players(id, game_state_id) ON DELETE CASCADE
);

CREATE INDEX wealth_game_state_id_idx ON game.wealth(game_state_id);

CREATE TABLE game.player_needs (
    player_id uuid PRIMARY KEY,
    game_state_id uuid NOT NULL,
    food_size text NOT NULL DEFAULT 'средний',
    food_per_day text,
    food_remaining numeric NOT NULL DEFAULT 0 CHECK (food_remaining >= 0),
    water_size text NOT NULL DEFAULT 'средний',
    water_per_day text,
    water_remaining numeric NOT NULL DEFAULT 0 CHECK (water_remaining >= 0),
    carry_capacity_max numeric NOT NULL DEFAULT 0 CHECK (carry_capacity_max >= 0),
    carry_current_weight numeric NOT NULL DEFAULT 0 CHECK (carry_current_weight >= 0),
    carry_unit text NOT NULL DEFAULT 'кг',
    movement_meters_per_turn integer NOT NULL DEFAULT 9 CHECK (movement_meters_per_turn >= 0),
    movement_km_per_day integer NOT NULL DEFAULT 36 CHECK (movement_km_per_day >= 0),
    CONSTRAINT player_needs_player_fkey FOREIGN KEY (player_id, game_state_id)
        REFERENCES game.players(id, game_state_id) ON DELETE CASCADE
);

CREATE INDEX player_needs_game_state_id_idx ON game.player_needs(game_state_id);

CREATE TABLE game.item_instances (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL REFERENCES game.game_states(id) ON DELETE CASCADE,
    template_id text,
    name text NOT NULL,
    item_type text NOT NULL,
    subtype text,
    description text,
    quantity integer NOT NULL DEFAULT 1 CHECK (quantity >= 0),
    stackable boolean NOT NULL DEFAULT false,
    weight_each numeric NOT NULL DEFAULT 0 CHECK (weight_each >= 0),
    condition text NOT NULL DEFAULT 'normal',
    rarity text NOT NULL DEFAULT 'common',
    is_magical boolean NOT NULL DEFAULT false,
    price_copper integer NOT NULL DEFAULT 0 CHECK (price_copper >= 0),
    price_silver integer NOT NULL DEFAULT 0 CHECK (price_silver >= 0),
    price_gold integer NOT NULL DEFAULT 0 CHECK (price_gold >= 0),
    price_platinum integer NOT NULL DEFAULT 0 CHECK (price_platinum >= 0),
    tags jsonb NOT NULL DEFAULT '[]'::jsonb,
    owner_kind text NOT NULL,
    owner_id uuid NOT NULL,
    CONSTRAINT item_instances_owner_kind_check CHECK (owner_kind IN ('player_inventory', 'location', 'world_object', 'container', 'npc')),
    CONSTRAINT item_instances_id_game_state_id_unique UNIQUE (id, game_state_id)
);

CREATE INDEX item_instances_game_state_id_idx ON game.item_instances(game_state_id);
CREATE INDEX item_instances_owner_idx ON game.item_instances(owner_kind, owner_id);
CREATE INDEX item_instances_item_type_idx ON game.item_instances(item_type);
CREATE INDEX item_instances_template_id_idx ON game.item_instances(template_id);

COMMENT ON TABLE game.item_instances IS 'Single table for all item copies in a save. Ownership is polymorphic through owner_kind/owner_id instead of separate inventory/location/container tables.';
COMMENT ON COLUMN game.item_instances.owner_kind IS 'Logical owner type: player_inventory, location, world_object, container, or npc.';
COMMENT ON COLUMN game.item_instances.owner_id IS 'ID of the owner row matching owner_kind, for example player id for player_inventory or world object id for world_object.';
COMMENT ON COLUMN game.item_instances.template_id IS 'Optional external/static item template identifier. Runtime identity is always item_instances.id.';
COMMENT ON COLUMN game.item_instances.tags IS 'Flexible JSON tags used by rules, UI filters, and AI context.';

CREATE TABLE game.item_weapon_stats (
    item_id uuid PRIMARY KEY,
    game_state_id uuid NOT NULL,
    damage_dice text NOT NULL,
    damage_type text NOT NULL,
    attack_bonus integer NOT NULL DEFAULT 0,
    damage_bonus integer NOT NULL DEFAULT 0,
    range text,
    properties jsonb NOT NULL DEFAULT '[]'::jsonb,
    CONSTRAINT item_weapon_stats_item_fkey FOREIGN KEY (item_id, game_state_id)
        REFERENCES game.item_instances(id, game_state_id) ON DELETE CASCADE
);

CREATE INDEX item_weapon_stats_game_state_id_idx ON game.item_weapon_stats(game_state_id);

CREATE TABLE game.item_armor_stats (
    item_id uuid PRIMARY KEY,
    game_state_id uuid NOT NULL,
    armor_class integer NOT NULL DEFAULT 0,
    armor_class_bonus integer NOT NULL DEFAULT 0,
    armor_type text,
    stealth_disadvantage boolean NOT NULL DEFAULT false,
    strength_requirement integer,
    CONSTRAINT item_armor_stats_item_fkey FOREIGN KEY (item_id, game_state_id)
        REFERENCES game.item_instances(id, game_state_id) ON DELETE CASCADE
);

CREATE INDEX item_armor_stats_game_state_id_idx ON game.item_armor_stats(game_state_id);

CREATE TABLE game.item_consumable_stats (
    item_id uuid PRIMARY KEY,
    game_state_id uuid NOT NULL,
    uses integer NOT NULL DEFAULT 1 CHECK (uses >= 0),
    effects jsonb NOT NULL DEFAULT '[]'::jsonb,
    CONSTRAINT item_consumable_stats_item_fkey FOREIGN KEY (item_id, game_state_id)
        REFERENCES game.item_instances(id, game_state_id) ON DELETE CASCADE
);

CREATE INDEX item_consumable_stats_game_state_id_idx ON game.item_consumable_stats(game_state_id);

CREATE TABLE game.equipped_gear (
    player_id uuid PRIMARY KEY,
    game_state_id uuid NOT NULL,
    head_item_id uuid REFERENCES game.item_instances(id) ON DELETE SET NULL,
    body_item_id uuid REFERENCES game.item_instances(id) ON DELETE SET NULL,
    hands_item_id uuid REFERENCES game.item_instances(id) ON DELETE SET NULL,
    legs_item_id uuid REFERENCES game.item_instances(id) ON DELETE SET NULL,
    feet_item_id uuid REFERENCES game.item_instances(id) ON DELETE SET NULL,
    main_hand_item_id uuid REFERENCES game.item_instances(id) ON DELETE SET NULL,
    off_hand_item_id uuid REFERENCES game.item_instances(id) ON DELETE SET NULL,
    amulet_item_id uuid REFERENCES game.item_instances(id) ON DELETE SET NULL,
    ring1_item_id uuid REFERENCES game.item_instances(id) ON DELETE SET NULL,
    ring2_item_id uuid REFERENCES game.item_instances(id) ON DELETE SET NULL,
    CONSTRAINT equipped_gear_player_fkey FOREIGN KEY (player_id, game_state_id)
        REFERENCES game.players(id, game_state_id) ON DELETE CASCADE
);

CREATE INDEX equipped_gear_game_state_id_idx ON game.equipped_gear(game_state_id);

COMMENT ON TABLE game.equipped_gear IS 'Equipment slots for one player. Slots store item UUID references, never item names.';
COMMENT ON COLUMN game.equipped_gear.main_hand_item_id IS 'Item equipped in the main hand. The item should belong to this player inventory before equip.';
COMMENT ON COLUMN game.equipped_gear.off_hand_item_id IS 'Item equipped in the off hand. The item should belong to this player inventory before equip.';

CREATE TABLE game.combat_stats (
    player_id uuid PRIMARY KEY,
    game_state_id uuid NOT NULL,
    armor_class integer NOT NULL DEFAULT 10,
    proficiency_bonus integer NOT NULL DEFAULT 2,
    in_combat boolean NOT NULL DEFAULT false,
    initiative_roll integer NOT NULL DEFAULT 0,
    CONSTRAINT combat_stats_player_fkey FOREIGN KEY (player_id, game_state_id)
        REFERENCES game.players(id, game_state_id) ON DELETE CASCADE
);

CREATE INDEX combat_stats_game_state_id_idx ON game.combat_stats(game_state_id);

CREATE TABLE game.attacks (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL,
    player_id uuid NOT NULL,
    item_id uuid REFERENCES game.item_instances(id) ON DELETE SET NULL,
    name text NOT NULL,
    roll text NOT NULL,
    damage text NOT NULL,
    damage_type text,
    CONSTRAINT attacks_player_fkey FOREIGN KEY (player_id, game_state_id)
        REFERENCES game.players(id, game_state_id) ON DELETE CASCADE
);

CREATE INDEX attacks_game_state_id_idx ON game.attacks(game_state_id);
CREATE INDEX attacks_player_id_idx ON game.attacks(player_id);

CREATE TABLE game.locations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL REFERENCES game.game_states(id) ON DELETE CASCADE,
    name text NOT NULL,
    description text,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT locations_id_game_state_id_unique UNIQUE (id, game_state_id)
);

CREATE INDEX locations_game_state_id_idx ON game.locations(game_state_id);

ALTER TABLE game.game_states
ADD CONSTRAINT game_states_current_location_id_fkey
FOREIGN KEY (current_location_id) REFERENCES game.locations(id) ON DELETE SET NULL;

CREATE TABLE game.location_exits (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL,
    location_id uuid NOT NULL,
    direction text NOT NULL,
    target_location_id uuid NOT NULL,
    description text,
    is_locked boolean NOT NULL DEFAULT false,
    CONSTRAINT location_exits_location_fkey FOREIGN KEY (location_id, game_state_id)
        REFERENCES game.locations(id, game_state_id) ON DELETE CASCADE,
    CONSTRAINT location_exits_target_location_fkey FOREIGN KEY (target_location_id, game_state_id)
        REFERENCES game.locations(id, game_state_id) ON DELETE CASCADE
);

CREATE INDEX location_exits_game_state_id_idx ON game.location_exits(game_state_id);
CREATE INDEX location_exits_location_id_idx ON game.location_exits(location_id);

CREATE TABLE game.world_objects (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL,
    location_id uuid NOT NULL,
    name text NOT NULL,
    object_type text NOT NULL,
    description text,
    state text NOT NULL DEFAULT 'обычное',
    tags jsonb NOT NULL DEFAULT '[]'::jsonb,
    CONSTRAINT world_objects_location_fkey FOREIGN KEY (location_id, game_state_id)
        REFERENCES game.locations(id, game_state_id) ON DELETE CASCADE,
    CONSTRAINT world_objects_id_game_state_id_unique UNIQUE (id, game_state_id)
);

CREATE INDEX world_objects_game_state_id_idx ON game.world_objects(game_state_id);
CREATE INDEX world_objects_location_id_idx ON game.world_objects(location_id);

CREATE TABLE game.world_containers (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL,
    location_id uuid NOT NULL,
    name text NOT NULL,
    description text,
    is_locked boolean NOT NULL DEFAULT false,
    key_item_id uuid REFERENCES game.item_instances(id) ON DELETE SET NULL,
    CONSTRAINT world_containers_location_fkey FOREIGN KEY (location_id, game_state_id)
        REFERENCES game.locations(id, game_state_id) ON DELETE CASCADE,
    CONSTRAINT world_containers_id_game_state_id_unique UNIQUE (id, game_state_id)
);

CREATE INDEX world_containers_game_state_id_idx ON game.world_containers(game_state_id);
CREATE INDEX world_containers_location_id_idx ON game.world_containers(location_id);

CREATE TABLE game.npcs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL REFERENCES game.game_states(id) ON DELETE CASCADE,
    location_id uuid REFERENCES game.locations(id) ON DELETE SET NULL,
    name text NOT NULL,
    role text,
    attitude text NOT NULL DEFAULT 'neutral',
    description text,
    is_alive boolean NOT NULL DEFAULT true,
    CONSTRAINT npcs_id_game_state_id_unique UNIQUE (id, game_state_id)
);

CREATE INDEX npcs_game_state_id_idx ON game.npcs(game_state_id);
CREATE INDEX npcs_location_id_idx ON game.npcs(location_id);

CREATE TABLE game.factions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL REFERENCES game.game_states(id) ON DELETE CASCADE,
    name text NOT NULL,
    reputation integer NOT NULL DEFAULT 0,
    description text
);

CREATE INDEX factions_game_state_id_idx ON game.factions(game_state_id);

CREATE TABLE game.quests (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL REFERENCES game.game_states(id) ON DELETE CASCADE,
    title text NOT NULL,
    description text,
    status text NOT NULL DEFAULT 'active',
    reward_experience integer NOT NULL DEFAULT 0 CHECK (reward_experience >= 0),
    reward_copper integer NOT NULL DEFAULT 0 CHECK (reward_copper >= 0),
    reward_silver integer NOT NULL DEFAULT 0 CHECK (reward_silver >= 0),
    reward_gold integer NOT NULL DEFAULT 0 CHECK (reward_gold >= 0),
    reward_platinum integer NOT NULL DEFAULT 0 CHECK (reward_platinum >= 0),
    CONSTRAINT quests_status_check CHECK (status IN ('active', 'completed', 'failed', 'hidden')),
    CONSTRAINT quests_id_game_state_id_unique UNIQUE (id, game_state_id)
);

CREATE INDEX quests_game_state_id_idx ON game.quests(game_state_id);

CREATE TABLE game.quest_steps (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL,
    quest_id uuid NOT NULL,
    description text NOT NULL,
    is_completed boolean NOT NULL DEFAULT false,
    sort_order integer NOT NULL DEFAULT 0,
    CONSTRAINT quest_steps_quest_fkey FOREIGN KEY (quest_id, game_state_id)
        REFERENCES game.quests(id, game_state_id) ON DELETE CASCADE
);

CREATE INDEX quest_steps_game_state_id_idx ON game.quest_steps(game_state_id);
CREATE INDEX quest_steps_quest_id_sort_order_idx ON game.quest_steps(quest_id, sort_order);

CREATE TABLE game.quest_reward_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL,
    quest_id uuid NOT NULL,
    item_id uuid NOT NULL REFERENCES game.item_instances(id) ON DELETE CASCADE,
    CONSTRAINT quest_reward_items_quest_fkey FOREIGN KEY (quest_id, game_state_id)
        REFERENCES game.quests(id, game_state_id) ON DELETE CASCADE
);

CREATE INDEX quest_reward_items_game_state_id_idx ON game.quest_reward_items(game_state_id);
CREATE INDEX quest_reward_items_quest_id_idx ON game.quest_reward_items(quest_id);

CREATE TABLE game.game_log_entries (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL REFERENCES game.game_states(id) ON DELETE CASCADE,
    turn_number integer NOT NULL CHECK (turn_number >= 0),
    type text NOT NULL,
    text text NOT NULL,
    important boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX game_log_entries_game_state_id_turn_number_idx ON game.game_log_entries(game_state_id, turn_number);
CREATE INDEX game_log_entries_type_idx ON game.game_log_entries(type);

CREATE TABLE game.game_turns (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL,
    account_id uuid NOT NULL,
    turn_number integer NOT NULL CHECK (turn_number >= 0),
    player_message text NOT NULL,
    master_answer text,
    raw_ai_response jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT game_turns_game_state_account_fkey FOREIGN KEY (game_state_id, account_id)
        REFERENCES game.game_states(id, account_id) ON DELETE CASCADE,
    CONSTRAINT game_turns_id_game_state_id_unique UNIQUE (id, game_state_id),
    CONSTRAINT game_turns_game_state_turn_number_unique UNIQUE (game_state_id, turn_number)
);

CREATE INDEX game_turns_game_state_id_idx ON game.game_turns(game_state_id);
CREATE INDEX game_turns_account_id_idx ON game.game_turns(account_id);

COMMENT ON TABLE game.game_turns IS 'Player turn history with the player message, master answer, and optional raw AI response.';
COMMENT ON COLUMN game.game_turns.raw_ai_response IS 'Raw AI response JSON for audit/debugging. Backend validates and stores extracted changes separately.';

CREATE TABLE game.game_changes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL,
    game_turn_id uuid NOT NULL,
    operation text NOT NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    status text NOT NULL DEFAULT 'pending',
    reject_reason text,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT game_changes_turn_fkey FOREIGN KEY (game_turn_id, game_state_id)
        REFERENCES game.game_turns(id, game_state_id) ON DELETE CASCADE,
    CONSTRAINT game_changes_status_check CHECK (status IN ('pending', 'applied', 'rejected'))
);

CREATE INDEX game_changes_game_state_id_idx ON game.game_changes(game_state_id);
CREATE INDEX game_changes_game_turn_id_idx ON game.game_changes(game_turn_id);
CREATE INDEX game_changes_status_idx ON game.game_changes(status);

COMMENT ON TABLE game.game_changes IS 'Validated change proposals from the AI master. The backend applies or rejects each row instead of letting AI mutate data directly.';
COMMENT ON COLUMN game.game_changes.operation IS 'Game operation name such as добавить_предмет, переместить_предмет, изменить_хп, or обновить_квест.';
COMMENT ON COLUMN game.game_changes.payload IS 'Raw structured payload for the operation. It is stored for audit and backend validation.';
COMMENT ON COLUMN game.game_changes.status IS 'Processing status: pending, applied, or rejected.';
COMMENT ON COLUMN game.game_changes.reject_reason IS 'Reason for rejection when status is rejected.';

CREATE OR REPLACE FUNCTION game.create_new_game(
    p_account_id uuid,
    p_save_name text DEFAULT 'Новая игра'
)
RETURNS uuid
LANGUAGE plpgsql
AS $$
DECLARE
    v_game_state_id uuid;
    v_player_id uuid;
    v_location_id uuid;
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM auth.accounts a
        WHERE a.id = p_account_id
          AND a.is_active
    ) THEN
        RAISE EXCEPTION 'Active account % does not exist', p_account_id
            USING ERRCODE = 'foreign_key_violation';
    END IF;

    INSERT INTO game.game_states (account_id, name)
    VALUES (p_account_id, COALESCE(NULLIF(btrim(p_save_name), ''), 'Новая игра'))
    RETURNING id INTO v_game_state_id;

    INSERT INTO game.players (game_state_id, account_id, name)
    VALUES (v_game_state_id, p_account_id, 'Герой')
    RETURNING id INTO v_player_id;

    INSERT INTO game.player_progression (player_id, game_state_id)
    VALUES (v_player_id, v_game_state_id);

    INSERT INTO game.player_resources (player_id, game_state_id)
    VALUES (v_player_id, v_game_state_id);

    INSERT INTO game.player_attributes (player_id, game_state_id)
    VALUES (v_player_id, v_game_state_id);

    INSERT INTO game.wealth (player_id, game_state_id)
    VALUES (v_player_id, v_game_state_id);

    INSERT INTO game.player_needs (player_id, game_state_id)
    VALUES (v_player_id, v_game_state_id);

    INSERT INTO game.equipped_gear (player_id, game_state_id)
    VALUES (v_player_id, v_game_state_id);

    INSERT INTO game.combat_stats (player_id, game_state_id)
    VALUES (v_player_id, v_game_state_id);

    INSERT INTO game.locations (game_state_id, name, description)
    VALUES (v_game_state_id, 'Начальная локация', 'Место, с которого начинается приключение.')
    RETURNING id INTO v_location_id;

    UPDATE game.game_states
    SET current_location_id = v_location_id
    WHERE id = v_game_state_id;

    INSERT INTO game.game_log_entries (game_state_id, turn_number, type, text, important)
    VALUES (v_game_state_id, 0, 'system', 'Создана новая игра.', true);

    RETURN v_game_state_id;
END;
$$;

CREATE OR REPLACE VIEW game.game_state_documents AS
SELECT
    gs.id AS game_state_id,
    gs.account_id,
    jsonb_build_object(
        'id', gs.id,
        'название', gs.name,
        'версиясхемы', gs.schema_version,
        'номерхода', gs.turn_number,
        'режим', gs.mode,
        'игрок', jsonb_build_object(
            'id', p.id,
            'персонаж', jsonb_build_object(
                'имя', p.name,
                'предыстория', p.background,
                'вид', p.species,
                'класс', p.class_name,
                'подкласс', p.subclass,
                'описание', p.description,
                'мировоззрение', p.alignment
            ),
            'прогресс', jsonb_build_object(
                'уровень', pp.level,
                'опыт', pp.experience,
                'опытдоследующегоуровня', pp.experience_to_next_level
            ),
            'ресурсы', jsonb_build_object(
                'хп', jsonb_build_object('максимум', pr.hp_max, 'текущее', pr.hp_current),
                'мана', jsonb_build_object('максимум', pr.mana_max, 'текущее', pr.mana_current),
                'очкидействий', jsonb_build_object('максимум', pr.action_points_max, 'текущее', pr.action_points_current),
                'состояния', COALESCE((
                    SELECT jsonb_agg(jsonb_build_object(
                        'id', c.id,
                        'название', c.name,
                        'тип', c.type,
                        'описание', c.description,
                        'источник', c.source,
                        'осталосьходов', c.remaining_turns,
                        'постоянное', c.is_permanent,
                        'стаки', c.stacks,
                        'максимумстаков', c.max_stacks,
                        'эффекты', c.effects,
                        'теги', c.tags
                    ) ORDER BY c.created_at, c.id)
                    FROM game.conditions c
                    WHERE c.player_id = p.id
                ), '[]'::jsonb),
                'ресурсыспособностей', COALESCE((
                    SELECT jsonb_agg(jsonb_build_object(
                        'id', lr.id,
                        'название', lr.name,
                        'максимум', lr.max_value,
                        'текущее', lr.current_value,
                        'восстановление', lr.recovery
                    ) ORDER BY lr.name, lr.id)
                    FROM game.limited_resources lr
                    WHERE lr.player_id = p.id
                ), '[]'::jsonb),
                'спасброскисмерти', jsonb_build_object(
                    'активны', pr.death_saves_active,
                    'успехи', pr.death_saves_successes,
                    'провалы', pr.death_saves_failures
                )
            ),
            'характеристики', jsonb_build_object(
                'сила', pa.strength,
                'ловкость', pa.dexterity,
                'телосложение', pa.constitution,
                'интеллект', pa.intelligence,
                'мудрость', pa.wisdom,
                'харизма', pa.charisma,
                'инициатива', pa.initiative,
                'скорость', pa.speed,
                'восприятие', pa.perception
            ),
            'владения', COALESCE((
                SELECT jsonb_object_agg(grouped.type, grouped.values)
                FROM (
                    SELECT prof.type, jsonb_agg(prof.value ORDER BY prof.value) AS values
                    FROM game.player_proficiencies prof
                    WHERE prof.player_id = p.id
                    GROUP BY prof.type
                ) grouped
            ), '{}'::jsonb),
            'способности', COALESCE((
                SELECT jsonb_object_agg(grouped.category, grouped.abilities)
                FROM (
                    SELECT ab.category, jsonb_agg(jsonb_build_object(
                        'id', ab.id,
                        'название', ab.name,
                        'описание', ab.description,
                        'тип', ab.ability_type,
                        'ресурсId', ab.cost_resource_id,
                        'стоимость', ab.cost_amount,
                        'эффекты', ab.effects
                    ) ORDER BY ab.name, ab.id) AS abilities
                    FROM game.abilities ab
                    WHERE ab.player_id = p.id
                    GROUP BY ab.category
                ) grouped
            ), '{}'::jsonb),
            'потребности', jsonb_build_object(
                'еда', jsonb_build_object('размер', pn.food_size, 'вдень', pn.food_per_day, 'остаток', pn.food_remaining),
                'вода', jsonb_build_object('размер', pn.water_size, 'вдень', pn.water_per_day, 'остаток', pn.water_remaining),
                'грузоподъемность', jsonb_build_object(
                    'максимум', pn.carry_capacity_max,
                    'текущийвес', pn.carry_current_weight,
                    'единица', pn.carry_unit
                ),
                'передвижение', jsonb_build_object(
                    'метровзаход', pn.movement_meters_per_turn,
                    'кмвдень', pn.movement_km_per_day
                )
            ),
            'богатство', jsonb_build_object(
                'монеты', jsonb_build_object(
                    'медные', w.copper,
                    'серебряные', w.silver,
                    'золотые', w.gold,
                    'платиновые', w.platinum
                )
            ),
            'инвентарь', COALESCE((
                SELECT jsonb_agg(jsonb_strip_nulls(jsonb_build_object(
                    'id', item.id,
                    'templateId', item.template_id,
                    'название', item.name,
                    'тип', item.item_type,
                    'подтип', item.subtype,
                    'описание', item.description,
                    'количество', item.quantity,
                    'стакуемый', item.stackable,
                    'вес', item.weight_each,
                    'состояние', item.condition,
                    'редкость', item.rarity,
                    'магический', item.is_magical,
                    'цена', jsonb_build_object(
                        'медные', item.price_copper,
                        'серебряные', item.price_silver,
                        'золотые', item.price_gold,
                        'платиновые', item.price_platinum
                    ),
                    'теги', item.tags,
                    'оружие', (
                        SELECT jsonb_build_object(
                            'уронКости', iws.damage_dice,
                            'типУрона', iws.damage_type,
                            'бонусАтаки', iws.attack_bonus,
                            'бонусУрона', iws.damage_bonus,
                            'дистанция', iws.range,
                            'свойства', iws.properties
                        )
                        FROM game.item_weapon_stats iws
                        WHERE iws.item_id = item.id
                    ),
                    'броня', (
                        SELECT jsonb_build_object(
                            'классдоспеха', ias.armor_class,
                            'бонусКД', ias.armor_class_bonus,
                            'типБрони', ias.armor_type,
                            'помехаСкрытности', ias.stealth_disadvantage,
                            'требованиеСилы', ias.strength_requirement
                        )
                        FROM game.item_armor_stats ias
                        WHERE ias.item_id = item.id
                    ),
                    'расходник', (
                        SELECT jsonb_build_object(
                            'использований', ics.uses,
                            'эффекты', ics.effects
                        )
                        FROM game.item_consumable_stats ics
                        WHERE ics.item_id = item.id
                    )
                )) ORDER BY item.name, item.id)
                FROM game.item_instances item
                WHERE item.game_state_id = gs.id
                  AND item.owner_kind = 'player_inventory'
                  AND item.owner_id = p.id
            ), '[]'::jsonb),
            'экипировка', jsonb_build_object(
                'голова', eg.head_item_id,
                'тело', eg.body_item_id,
                'руки', eg.hands_item_id,
                'ноги', eg.legs_item_id,
                'обувь', eg.feet_item_id,
                'основнаярука', eg.main_hand_item_id,
                'втораярука', eg.off_hand_item_id,
                'амулет', eg.amulet_item_id,
                'кольцо1', eg.ring1_item_id,
                'кольцо2', eg.ring2_item_id
            ),
            'бой', jsonb_build_object(
                'классдоспеха', cs.armor_class,
                'бонусмастерства', cs.proficiency_bonus,
                'вбою', cs.in_combat,
                'бросокинициативы', cs.initiative_roll,
                'атаки', COALESCE((
                    SELECT jsonb_agg(jsonb_build_object(
                        'id', atk.id,
                        'предметId', atk.item_id,
                        'название', atk.name,
                        'бросок', atk.roll,
                        'урон', atk.damage,
                        'типУрона', atk.damage_type
                    ) ORDER BY atk.name, atk.id)
                    FROM game.attacks atk
                    WHERE atk.player_id = p.id
                ), '[]'::jsonb)
            )
        ),
        'мир', jsonb_build_object(
            'текущаялокацияId', gs.current_location_id,
            'локации', COALESCE((
                SELECT jsonb_object_agg(loc.id::text, jsonb_build_object(
                    'id', loc.id,
                    'название', loc.name,
                    'описание', loc.description,
                    'выходы', COALESCE((
                        SELECT jsonb_agg(jsonb_build_object(
                            'id', le.id,
                            'направление', le.direction,
                            'целеваялокацияId', le.target_location_id,
                            'описание', le.description,
                            'заперто', le.is_locked
                        ) ORDER BY le.direction, le.id)
                        FROM game.location_exits le
                        WHERE le.location_id = loc.id
                    ), '[]'::jsonb),
                    'объекты', COALESCE((
                        SELECT jsonb_agg(jsonb_build_object(
                            'id', wo.id,
                            'название', wo.name,
                            'тип', wo.object_type,
                            'описание', wo.description,
                            'состояние', wo.state,
                            'теги', wo.tags
                        ) ORDER BY wo.name, wo.id)
                        FROM game.world_objects wo
                        WHERE wo.location_id = loc.id
                    ), '[]'::jsonb),
                    'контейнеры', COALESCE((
                        SELECT jsonb_agg(jsonb_build_object(
                            'id', wc.id,
                            'название', wc.name,
                            'описание', wc.description,
                            'заперт', wc.is_locked,
                            'ключПредметId', wc.key_item_id
                        ) ORDER BY wc.name, wc.id)
                        FROM game.world_containers wc
                        WHERE wc.location_id = loc.id
                    ), '[]'::jsonb)
                ) ORDER BY loc.name, loc.id)
                FROM game.locations loc
                WHERE loc.game_state_id = gs.id
            ), '{}'::jsonb),
            'нпс', COALESCE((
                SELECT jsonb_object_agg(npc.id::text, jsonb_build_object(
                    'id', npc.id,
                    'локацияId', npc.location_id,
                    'имя', npc.name,
                    'роль', npc.role,
                    'отношение', npc.attitude,
                    'описание', npc.description,
                    'жив', npc.is_alive
                ) ORDER BY npc.name, npc.id)
                FROM game.npcs npc
                WHERE npc.game_state_id = gs.id
            ), '{}'::jsonb),
            'фракции', COALESCE((
                SELECT jsonb_object_agg(f.id::text, jsonb_build_object(
                    'id', f.id,
                    'название', f.name,
                    'репутация', f.reputation,
                    'описание', f.description
                ) ORDER BY f.name, f.id)
                FROM game.factions f
                WHERE f.game_state_id = gs.id
            ), '{}'::jsonb)
        ),
        'квесты', COALESCE((
            SELECT jsonb_agg(jsonb_build_object(
                'id', q.id,
                'название', q.title,
                'описание', q.description,
                'статус', q.status,
                'награда', jsonb_build_object(
                    'опыт', q.reward_experience,
                    'медные', q.reward_copper,
                    'серебряные', q.reward_silver,
                    'золотые', q.reward_gold,
                    'платиновые', q.reward_platinum
                ),
                'шаги', COALESCE((
                    SELECT jsonb_agg(jsonb_build_object(
                        'id', qs.id,
                        'описание', qs.description,
                        'выполнен', qs.is_completed,
                        'порядок', qs.sort_order
                    ) ORDER BY qs.sort_order, qs.id)
                    FROM game.quest_steps qs
                    WHERE qs.quest_id = q.id
                ), '[]'::jsonb),
                'предметынаграды', COALESCE((
                    SELECT jsonb_agg(qri.item_id ORDER BY qri.id)
                    FROM game.quest_reward_items qri
                    WHERE qri.quest_id = q.id
                ), '[]'::jsonb)
            ) ORDER BY q.title, q.id)
            FROM game.quests q
            WHERE q.game_state_id = gs.id
        ), '[]'::jsonb),
        'история', COALESCE((
            SELECT jsonb_agg(jsonb_build_object(
                'id', gle.id,
                'номерхода', gle.turn_number,
                'тип', gle.type,
                'текст', gle.text,
                'важное', gle.important,
                'создано', gle.created_at
            ) ORDER BY gle.turn_number, gle.created_at, gle.id)
            FROM game.game_log_entries gle
            WHERE gle.game_state_id = gs.id
        ), '[]'::jsonb)
    ) AS data
FROM game.game_states gs
LEFT JOIN LATERAL (
    SELECT p_inner.*
    FROM game.players p_inner
    WHERE p_inner.game_state_id = gs.id
    ORDER BY p_inner.created_at, p_inner.id
    LIMIT 1
) p ON true
LEFT JOIN game.player_progression pp ON pp.player_id = p.id
LEFT JOIN game.player_resources pr ON pr.player_id = p.id
LEFT JOIN game.player_attributes pa ON pa.player_id = p.id
LEFT JOIN game.wealth w ON w.player_id = p.id
LEFT JOIN game.player_needs pn ON pn.player_id = p.id
LEFT JOIN game.equipped_gear eg ON eg.player_id = p.id
LEFT JOIN game.combat_stats cs ON cs.player_id = p.id;
