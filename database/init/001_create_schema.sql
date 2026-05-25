CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE SCHEMA IF NOT EXISTS game;

CREATE TYPE game.ability_source AS ENUM ('class', 'species', 'feat');

CREATE TABLE game.players (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name text NOT NULL DEFAULT '',
    background text NOT NULL DEFAULT '',
    species text NOT NULL DEFAULT '',
    class_name text NOT NULL DEFAULT '',
    subclass text NOT NULL DEFAULT '',
    level integer NOT NULL DEFAULT 0 CHECK (level >= 0),
    experience integer NOT NULL DEFAULT 0 CHECK (experience >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE game.resources (
    player_id uuid PRIMARY KEY REFERENCES game.players(id) ON DELETE CASCADE,
    hp_max integer NOT NULL DEFAULT 0 CHECK (hp_max >= 0),
    hp_current integer NOT NULL DEFAULT 0 CHECK (hp_current >= 0),
    mana_max integer NOT NULL DEFAULT 0 CHECK (mana_max >= 0),
    mana_current integer NOT NULL DEFAULT 0 CHECK (mana_current >= 0),
    action_points_max integer NOT NULL DEFAULT 0 CHECK (action_points_max >= 0),
    action_points_current integer NOT NULL DEFAULT 0 CHECK (action_points_current >= 0)
);

CREATE TABLE game.conditions (
    id bigserial PRIMARY KEY,
    player_id uuid NOT NULL REFERENCES game.players(id) ON DELETE CASCADE,
    name text NOT NULL DEFAULT '',
    description text NOT NULL DEFAULT '',
    duration_turns integer CHECK (duration_turns IS NULL OR duration_turns >= 0),
    power integer,
    is_permanent boolean NOT NULL DEFAULT false
);

CREATE TABLE game.attributes (
    player_id uuid PRIMARY KEY REFERENCES game.players(id) ON DELETE CASCADE,
    strength integer NOT NULL DEFAULT 0,
    intelligence integer NOT NULL DEFAULT 0,
    dexterity integer NOT NULL DEFAULT 0,
    wisdom integer NOT NULL DEFAULT 0,
    constitution integer NOT NULL DEFAULT 0,
    charisma integer NOT NULL DEFAULT 0,
    initiative integer NOT NULL DEFAULT 0,
    speed integer NOT NULL DEFAULT 0,
    perception integer NOT NULL DEFAULT 0
);

CREATE TABLE game.equipment_proficiencies (
    player_id uuid PRIMARY KEY REFERENCES game.players(id) ON DELETE CASCADE,
    armor_light boolean NOT NULL DEFAULT false,
    armor_medium boolean NOT NULL DEFAULT false,
    armor_heavy boolean NOT NULL DEFAULT false,
    armor_shields boolean NOT NULL DEFAULT false,
    weapon_simple boolean NOT NULL DEFAULT false,
    weapon_martial boolean NOT NULL DEFAULT false
);

CREATE TABLE game.weapon_other_proficiencies (
    id bigserial PRIMARY KEY,
    player_id uuid NOT NULL REFERENCES game.players(id) ON DELETE CASCADE,
    name text NOT NULL,
    UNIQUE (player_id, name)
);

CREATE TABLE game.abilities (
    id bigserial PRIMARY KEY,
    player_id uuid NOT NULL REFERENCES game.players(id) ON DELETE CASCADE,
    source game.ability_source NOT NULL,
    name text NOT NULL DEFAULT '',
    description text NOT NULL DEFAULT ''
);

CREATE TABLE game.needs (
    player_id uuid PRIMARY KEY REFERENCES game.players(id) ON DELETE CASCADE,
    food_size text NOT NULL DEFAULT 'medium',
    food_per_day text NOT NULL DEFAULT '',
    water_size text NOT NULL DEFAULT 'medium',
    water_per_day text NOT NULL DEFAULT '',
    carrying_capacity_max integer NOT NULL DEFAULT 0 CHECK (carrying_capacity_max >= 0),
    carrying_capacity_unit text NOT NULL DEFAULT 'kg'
);

CREATE TABLE game.movement (
    player_id uuid PRIMARY KEY REFERENCES game.players(id) ON DELETE CASCADE,
    km_per_turn integer NOT NULL DEFAULT 0 CHECK (km_per_turn >= 0)
);

CREATE TABLE game.wealth (
    player_id uuid PRIMARY KEY REFERENCES game.players(id) ON DELETE CASCADE,
    copper integer NOT NULL DEFAULT 0 CHECK (copper >= 0),
    silver integer NOT NULL DEFAULT 0 CHECK (silver >= 0),
    gold integer NOT NULL DEFAULT 0 CHECK (gold >= 0),
    platinum integer NOT NULL DEFAULT 0 CHECK (platinum >= 0)
);

CREATE TABLE game.inventory_items (
    id bigserial PRIMARY KEY,
    player_id uuid NOT NULL REFERENCES game.players(id) ON DELETE CASCADE,
    item_order integer NOT NULL DEFAULT 0 CHECK (item_order >= 0),
    name text NOT NULL
);

CREATE TABLE game.equipped_gear (
    player_id uuid PRIMARY KEY REFERENCES game.players(id) ON DELETE CASCADE,
    head text,
    body text,
    hands text,
    legs text,
    feet text,
    main_hand text,
    off_hand text,
    amulet text,
    ring1 text,
    ring2 text
);

CREATE INDEX conditions_player_id_idx ON game.conditions(player_id);
CREATE INDEX abilities_player_id_source_idx ON game.abilities(player_id, source);
CREATE INDEX inventory_items_player_id_order_idx ON game.inventory_items(player_id, item_order, id);
CREATE INDEX weapon_other_proficiencies_player_id_idx ON game.weapon_other_proficiencies(player_id);

CREATE OR REPLACE FUNCTION game.set_updated_at()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$;

CREATE TRIGGER players_set_updated_at
BEFORE UPDATE ON game.players
FOR EACH ROW
EXECUTE FUNCTION game.set_updated_at();

CREATE OR REPLACE FUNCTION game.create_player_defaults()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO game.resources (player_id) VALUES (NEW.id);
    INSERT INTO game.attributes (player_id) VALUES (NEW.id);
    INSERT INTO game.equipment_proficiencies (player_id) VALUES (NEW.id);
    INSERT INTO game.needs (player_id) VALUES (NEW.id);
    INSERT INTO game.movement (player_id) VALUES (NEW.id);
    INSERT INTO game.wealth (player_id) VALUES (NEW.id);
    INSERT INTO game.equipped_gear (player_id) VALUES (NEW.id);
    RETURN NEW;
END;
$$;

CREATE TRIGGER players_create_defaults
AFTER INSERT ON game.players
FOR EACH ROW
EXECUTE FUNCTION game.create_player_defaults();

CREATE OR REPLACE VIEW game.player_documents AS
SELECT
    p.id AS player_id,
    jsonb_build_object(
        'Character', jsonb_build_object(
            'Name', p.name,
            'Background', p.background,
            'Species', p.species,
            'Class', p.class_name,
            'Subclass', p.subclass,
            'Level', p.level,
            'Experience', p.experience
        ),
        'Resources', jsonb_build_object(
            'Hp', jsonb_build_object('Max', r.hp_max, 'Current', r.hp_current),
            'Mana', jsonb_build_object('Max', r.mana_max, 'Current', r.mana_current),
            'ActionPoints', jsonb_build_object('Max', r.action_points_max, 'Current', r.action_points_current),
            'Conditions', jsonb_build_object(
                'Active', COALESCE((
                    SELECT jsonb_agg(jsonb_build_object(
                        'Name', c.name,
                        'Description', c.description,
                        'DurationTurns', c.duration_turns,
                        'Power', c.power,
                        'IsPermanent', c.is_permanent
                    ) ORDER BY c.id)
                    FROM game.conditions c
                    WHERE c.player_id = p.id
                ), '[]'::jsonb)
            )
        ),
        'Attributes', jsonb_build_object(
            'Strength', a.strength,
            'Intelligence', a.intelligence,
            'Dexterity', a.dexterity,
            'Wisdom', a.wisdom,
            'Constitution', a.constitution,
            'Charisma', a.charisma,
            'Initiative', a.initiative,
            'Speed', a.speed,
            'Perception', a.perception
        ),
        'EquipmentProficiency', jsonb_build_object(
            'Armor', jsonb_build_object(
                'Light', ep.armor_light,
                'Medium', ep.armor_medium,
                'Heavy', ep.armor_heavy,
                'Shields', ep.armor_shields
            ),
            'Weapons', jsonb_build_object(
                'Simple', ep.weapon_simple,
                'Martial', ep.weapon_martial,
                'Other', COALESCE((
                    SELECT jsonb_agg(wop.name ORDER BY wop.name)
                    FROM game.weapon_other_proficiencies wop
                    WHERE wop.player_id = p.id
                ), '[]'::jsonb)
            )
        ),
        'Abilities', jsonb_build_object(
            'ClassAbilities', COALESCE((
                SELECT jsonb_agg(jsonb_build_object('Name', ab.name, 'Description', ab.description) ORDER BY ab.id)
                FROM game.abilities ab
                WHERE ab.player_id = p.id AND ab.source = 'class'
            ), '[]'::jsonb),
            'SpeciesAbilities', COALESCE((
                SELECT jsonb_agg(jsonb_build_object('Name', ab.name, 'Description', ab.description) ORDER BY ab.id)
                FROM game.abilities ab
                WHERE ab.player_id = p.id AND ab.source = 'species'
            ), '[]'::jsonb),
            'Feats', COALESCE((
                SELECT jsonb_agg(jsonb_build_object('Name', ab.name, 'Description', ab.description) ORDER BY ab.id)
                FROM game.abilities ab
                WHERE ab.player_id = p.id AND ab.source = 'feat'
            ), '[]'::jsonb)
        ),
        'Needs', jsonb_build_object(
            'Food', jsonb_build_object('Size', n.food_size, 'PerDay', n.food_per_day),
            'Water', jsonb_build_object('Size', n.water_size, 'PerDay', n.water_per_day),
            'CarryingCapacity', jsonb_build_object('Max', n.carrying_capacity_max, 'Unit', n.carrying_capacity_unit)
        ),
        'Movement', jsonb_build_object(
            'KmPerTurn', m.km_per_turn
        ),
        'Wealth', jsonb_build_object(
            'Coins', jsonb_build_object(
                'Copper', w.copper,
                'Silver', w.silver,
                'Gold', w.gold,
                'Platinum', w.platinum
            )
        ),
        'Inventory', COALESCE((
            SELECT jsonb_agg(ii.name ORDER BY ii.item_order, ii.id)
            FROM game.inventory_items ii
            WHERE ii.player_id = p.id
        ), '[]'::jsonb),
        'EquippedGear', jsonb_build_object(
            'Head', eg.head,
            'Body', eg.body,
            'Hands', eg.hands,
            'Legs', eg.legs,
            'Feet', eg.feet,
            'MainHand', eg.main_hand,
            'OffHand', eg.off_hand,
            'Amulet', eg.amulet,
            'Ring1', eg.ring1,
            'Ring2', eg.ring2
        )
    ) AS data
FROM game.players p
JOIN game.resources r ON r.player_id = p.id
JOIN game.attributes a ON a.player_id = p.id
JOIN game.equipment_proficiencies ep ON ep.player_id = p.id
JOIN game.needs n ON n.player_id = p.id
JOIN game.movement m ON m.player_id = p.id
JOIN game.wealth w ON w.player_id = p.id
JOIN game.equipped_gear eg ON eg.player_id = p.id;
