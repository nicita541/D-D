CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE SCHEMA IF NOT EXISTS game;

CREATE TABLE IF NOT EXISTS game.combat_stats (
    player_id uuid PRIMARY KEY REFERENCES game.players(id) ON DELETE CASCADE,
    armor_class integer NOT NULL DEFAULT 10,
    proficiency_bonus integer NOT NULL DEFAULT 2
);

CREATE TABLE IF NOT EXISTS game.attacks (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    player_id uuid NOT NULL REFERENCES game.players(id) ON DELETE CASCADE,
    attack_order integer NOT NULL DEFAULT 0,
    name text NOT NULL,
    roll text NOT NULL DEFAULT '',
    damage text NOT NULL DEFAULT ''
);

ALTER TABLE game.inventory_items
    ADD COLUMN IF NOT EXISTS item_type text,
    ADD COLUMN IF NOT EXISTS damage text,
    ADD COLUMN IF NOT EXISTS weight double precision,
    ADD COLUMN IF NOT EXISTS quantity integer,
    ADD COLUMN IF NOT EXISTS armor_class integer,
    ADD COLUMN IF NOT EXISTS armor_class_bonus integer;

CREATE INDEX IF NOT EXISTS attacks_player_id_order_idx ON game.attacks(player_id, attack_order, id);

INSERT INTO game.combat_stats (player_id)
SELECT id
FROM game.players
ON CONFLICT (player_id) DO NOTHING;

CREATE OR REPLACE FUNCTION game.create_player_defaults()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO game.resources (player_id) VALUES (NEW.id) ON CONFLICT (player_id) DO NOTHING;
    INSERT INTO game.attributes (player_id) VALUES (NEW.id) ON CONFLICT (player_id) DO NOTHING;
    INSERT INTO game.equipment_proficiencies (player_id) VALUES (NEW.id) ON CONFLICT (player_id) DO NOTHING;
    INSERT INTO game.needs (player_id) VALUES (NEW.id) ON CONFLICT (player_id) DO NOTHING;
    INSERT INTO game.movement (player_id) VALUES (NEW.id) ON CONFLICT (player_id) DO NOTHING;
    INSERT INTO game.wealth (player_id) VALUES (NEW.id) ON CONFLICT (player_id) DO NOTHING;
    INSERT INTO game.equipped_gear (player_id) VALUES (NEW.id) ON CONFLICT (player_id) DO NOTHING;
    INSERT INTO game.combat_stats (player_id) VALUES (NEW.id) ON CONFLICT (player_id) DO NOTHING;
    RETURN NEW;
END;
$$;

CREATE OR REPLACE VIEW game.player_documents AS
SELECT
    p.id AS player_id,
    jsonb_build_object(
        'персонаж', jsonb_build_object(
            'имя', p.name,
            'предыстория', p.background,
            'вид', p.species,
            'класс', p.class_name,
            'подкласс', p.subclass,
            'уровень', p.level,
            'опыт', p.experience
        ),
        'ресурсы', jsonb_build_object(
            'хп', jsonb_build_object('максимум', r.hp_max, 'текущее', r.hp_current),
            'мана', jsonb_build_object('максимум', r.mana_max, 'текущее', r.mana_current),
            'очкидействий', jsonb_build_object('максимум', r.action_points_max, 'текущее', r.action_points_current),
            'состояния', jsonb_build_object(
                'ошеломление', false,
                'другие', COALESCE((
                    SELECT jsonb_agg(jsonb_build_object(
                        'название', c.name,
                        'описание', c.description,
                        'длительностьходов', c.duration_turns,
                        'сила', c.power,
                        'постоянное', c.is_permanent
                    ) ORDER BY c.id)
                    FROM game.conditions c
                    WHERE c.player_id = p.id
                ), '[]'::jsonb)
            )
        ),
        'характеристики', jsonb_build_object(
            'сила', a.strength,
            'интеллект', a.intelligence,
            'ловкость', a.dexterity,
            'мудрость', a.wisdom,
            'телосложение', a.constitution,
            'харизма', a.charisma,
            'инициатива', a.initiative,
            'скорость', a.speed,
            'восприятие', a.perception
        ),
        'владениеснаряжением', jsonb_build_object(
            'доспехи', jsonb_build_object(
                'легкие', ep.armor_light,
                'средние', ep.armor_medium,
                'тяжелые', ep.armor_heavy,
                'щиты', ep.armor_shields
            ),
            'оружие', jsonb_build_object(
                'простое', ep.weapon_simple,
                'воинское', ep.weapon_martial,
                'другое', COALESCE((
                    SELECT jsonb_agg(wop.name ORDER BY wop.name)
                    FROM game.weapon_other_proficiencies wop
                    WHERE wop.player_id = p.id
                ), '[]'::jsonb)
            )
        ),
        'способности', jsonb_build_object(
            'способностикласса', COALESCE((
                SELECT jsonb_agg(jsonb_build_object('название', ab.name, 'описание', ab.description) ORDER BY ab.id)
                FROM game.abilities ab
                WHERE ab.player_id = p.id AND ab.source = 'class'
            ), '[]'::jsonb),
            'видовыеспособности', COALESCE((
                SELECT jsonb_agg(jsonb_build_object('название', ab.name, 'описание', ab.description) ORDER BY ab.id)
                FROM game.abilities ab
                WHERE ab.player_id = p.id AND ab.source = 'species'
            ), '[]'::jsonb),
            'черты', COALESCE((
                SELECT jsonb_agg(jsonb_build_object('название', ab.name, 'описание', ab.description) ORDER BY ab.id)
                FROM game.abilities ab
                WHERE ab.player_id = p.id AND ab.source = 'feat'
            ), '[]'::jsonb)
        ),
        'потребности', jsonb_build_object(
            'сколькоест', jsonb_build_object('размер', n.food_size, 'вдень', n.food_per_day),
            'сколькопьет', jsonb_build_object('размер', n.water_size, 'вдень', n.water_per_day),
            'грузоподъемность', jsonb_build_object('максимум', n.carrying_capacity_max, 'единица', n.carrying_capacity_unit)
        ),
        'передвижение', jsonb_build_object(
            'кмзаход', m.km_per_turn
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
                'название', ii.name,
                'тип', ii.item_type,
                'урон', ii.damage,
                'вес', ii.weight,
                'количество', ii.quantity,
                'КД', ii.armor_class,
                'бонусКД', ii.armor_class_bonus
            )) ORDER BY ii.item_order, ii.id)
            FROM game.inventory_items ii
            WHERE ii.player_id = p.id
        ), '[]'::jsonb),
        'снаряжениенадетое', jsonb_build_object(
            'голова', eg.head,
            'тело', eg.body,
            'руки', eg.hands,
            'ноги', eg.legs,
            'обувь', eg.feet,
            'основнаярука', eg.main_hand,
            'вторая_рука', eg.off_hand,
            'амулет', eg.amulet,
            'кольцо_1', eg.ring1,
            'кольцо_2', eg.ring2
        ),
        'боевыепараметры', jsonb_build_object(
            'классдоспеха', cs.armor_class,
            'бонусмастерства', cs.proficiency_bonus,
            'атаки', COALESCE((
                SELECT jsonb_agg(jsonb_build_object(
                    'название', atk.name,
                    'бросок', atk.roll,
                    'урон', atk.damage
                ) ORDER BY atk.attack_order, atk.id)
                FROM game.attacks atk
                WHERE atk.player_id = p.id
            ), '[]'::jsonb)
        )
    ) AS data
FROM game.players p
JOIN game.resources r ON r.player_id = p.id
JOIN game.attributes a ON a.player_id = p.id
JOIN game.equipment_proficiencies ep ON ep.player_id = p.id
JOIN game.needs n ON n.player_id = p.id
JOIN game.movement m ON m.player_id = p.id
JOIN game.wealth w ON w.player_id = p.id
JOIN game.equipped_gear eg ON eg.player_id = p.id
JOIN game.combat_stats cs ON cs.player_id = p.id;
