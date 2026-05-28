\set ON_ERROR_STOP on

BEGIN;

CREATE TEMP TABLE torven_context (
    account_id uuid,
    game_state_id uuid,
    player_id uuid,
    location_id uuid,
    well_id uuid
) ON COMMIT DROP;

CREATE TEMP TABLE torven_items (
    code text PRIMARY KEY,
    item_id uuid NOT NULL
) ON COMMIT DROP;

WITH account_row AS (
    INSERT INTO auth.accounts (email, username, password_hash, display_name)
    VALUES ('test@example.com', 'test', 'fake_hash_for_local_seed_only', 'Тестовый игрок')
    ON CONFLICT (email) DO UPDATE
    SET username = EXCLUDED.username,
        password_hash = EXCLUDED.password_hash,
        display_name = EXCLUDED.display_name
    RETURNING id
),
game_row AS (
    SELECT game.create_new_game(id, 'Торвен: старт') AS game_state_id, id AS account_id
    FROM account_row
)
INSERT INTO torven_context (account_id, game_state_id)
SELECT account_id, game_state_id
FROM game_row;

UPDATE torven_context ctx
SET player_id = p.id
FROM game.players p
WHERE p.game_state_id = ctx.game_state_id;

UPDATE game.players p
SET name = 'Торвен Сталегрив',
    background = 'Бывший солдат приграничного гарнизона, переживший нападение орков на свою крепость.',
    species = 'Человек',
    class_name = 'Воин',
    subclass = 'Мастер боевых искусств',
    description = 'Закалённый воин с тяжёлым взглядом и привычкой проверять рукоять меча перед каждым разговором.',
    alignment = 'нейтрально-добрый'
FROM torven_context ctx
WHERE p.id = ctx.player_id;

UPDATE game.player_progression pp
SET level = 1,
    experience = 0,
    experience_to_next_level = 300
FROM torven_context ctx
WHERE pp.player_id = ctx.player_id;

UPDATE game.player_resources pr
SET hp_max = 12,
    hp_current = 12,
    mana_max = 0,
    mana_current = 0,
    action_points_max = 1,
    action_points_current = 1
FROM torven_context ctx
WHERE pr.player_id = ctx.player_id;

UPDATE game.player_attributes pa
SET strength = 16,
    dexterity = 13,
    constitution = 15,
    intelligence = 10,
    wisdom = 12,
    charisma = 8,
    initiative = 1,
    speed = 9,
    perception = 12
FROM torven_context ctx
WHERE pa.player_id = ctx.player_id;

UPDATE game.wealth w
SET gold = 25
FROM torven_context ctx
WHERE w.player_id = ctx.player_id;

UPDATE game.player_needs pn
SET food_size = 'средний',
    food_per_day = '2 рациона',
    food_remaining = 7,
    water_size = 'средний',
    water_per_day = '3 литра',
    water_remaining = 1,
    carry_capacity_max = 120,
    carry_current_weight = 39.5,
    carry_unit = 'кг',
    movement_meters_per_turn = 9,
    movement_km_per_day = 36
FROM torven_context ctx
WHERE pn.player_id = ctx.player_id;

INSERT INTO game.player_proficiencies (game_state_id, player_id, type, value)
SELECT ctx.game_state_id, ctx.player_id, prof.type, prof.value
FROM torven_context ctx
CROSS JOIN (
    VALUES
        ('armor', 'light'),
        ('armor', 'medium'),
        ('armor', 'heavy'),
        ('armor', 'shield'),
        ('weapon', 'simple'),
        ('weapon', 'martial'),
        ('saving_throw', 'strength'),
        ('saving_throw', 'constitution'),
        ('language', 'common')
) AS prof(type, value)
ON CONFLICT (player_id, type, value) DO NOTHING;

INSERT INTO game.abilities (game_state_id, player_id, category, name, description, ability_type, effects)
SELECT ctx.game_state_id, ctx.player_id, ab.category, ab.name, ab.description, ab.ability_type, ab.effects::jsonb
FROM torven_context ctx
CROSS JOIN (
    VALUES
        ('class', 'Боевой стиль: Защита', '+1 к КД при ношении доспеха.', 'passive', '[{"type":"armor_class_bonus","value":1}]'),
        ('class', 'Второе дыхание', '1 раз за короткий отдых восстанавливает 1d10 + уровень ХП.', 'bonus_action', '[]'),
        ('species', 'Универсальность человека', '+1 ко всем характеристикам.', 'passive', '[]'),
        ('feat', 'Выносливый', 'Привык к долгим походам и тяжёлым условиям.', 'passive', '[]')
) AS ab(category, name, description, ability_type, effects);

UPDATE game.locations l
SET name = 'Старая дорога у колодца',
    description = 'Разбитая дорога проходит мимо старого каменного колодца. Ветер шевелит сухую траву у обочины.'
FROM torven_context ctx
WHERE l.id = (
    SELECT gs.current_location_id
    FROM game.game_states gs
    WHERE gs.id = ctx.game_state_id
);

UPDATE torven_context ctx
SET location_id = gs.current_location_id
FROM game.game_states gs
WHERE gs.id = ctx.game_state_id;

WITH inserted_well AS (
    INSERT INTO game.world_objects (game_state_id, location_id, name, object_type, description, state, tags)
    SELECT game_state_id,
           location_id,
           'Старый колодец',
           'колодец',
           'Глубокий колодец из потемневшего камня. Со дна тянет холодом.',
           'обычное',
           '["well","deep","stone"]'::jsonb
    FROM torven_context
    RETURNING id
)
UPDATE torven_context
SET well_id = inserted_well.id
FROM inserted_well;

WITH inserted_items AS (
    INSERT INTO game.item_instances (
        game_state_id,
        template_id,
        name,
        item_type,
        subtype,
        description,
        quantity,
        stackable,
        weight_each,
        rarity,
        price_gold,
        tags,
        owner_kind,
        owner_id
    )
    SELECT ctx.game_state_id, item.template_id, item.name, item.item_type, item.subtype, item.description,
           item.quantity, item.stackable, item.weight_each, item.rarity, item.price_gold,
           item.tags::jsonb, 'player_inventory', ctx.player_id
    FROM torven_context ctx
    CROSS JOIN (
        VALUES
            ('torven_longsword_001', 'longsword', 'Длинный меч', 'weapon', 'martial_melee', 'Надёжный солдатский меч.', 1, false, 1.5, 'common', 15, '["weapon","sword"]'),
            ('torven_shield_001', 'shield', 'Щит', 'armor', 'shield', 'Простой деревянный щит с металлической окантовкой.', 1, false, 3, 'common', 10, '["shield"]'),
            ('torven_chainmail_001', 'chainmail', 'Кольчуга', 'armor', 'heavy', 'Тяжёлая кольчуга, пережившая не один бой.', 1, false, 25, 'common', 75, '["armor","heavy"]'),
            ('torven_dagger_001', 'dagger', 'Кинжал', 'weapon', 'simple_melee', 'Короткий клинок для ближнего боя.', 1, false, 0.5, 'common', 2, '["weapon","dagger"]'),
            ('torven_rations_001', 'rations', 'Рацион', 'consumable', 'food', 'Сухой дорожный паёк.', 7, true, 1, 'common', 0, '["food"]'),
            ('torven_torches_001', 'torch', 'Факел', 'gear', 'light', 'Связка просмолённых факелов.', 5, true, 0.5, 'common', 0, '["light"]'),
            ('torven_waterskin_001', 'waterskin', 'Бурдюк', 'gear', 'water', 'Кожаный бурдюк с водой.', 1, false, 1, 'common', 0, '["water"]')
    ) AS item(template_id, code, name, item_type, subtype, description, quantity, stackable, weight_each, rarity, price_gold, tags)
    RETURNING template_id, id
)
INSERT INTO torven_items (code, item_id)
SELECT split_part(template_id, '_', 2), id
FROM inserted_items;

INSERT INTO game.item_weapon_stats (game_state_id, item_id, damage_dice, damage_type, attack_bonus, damage_bonus, properties)
SELECT ctx.game_state_id, ti.item_id, stats.damage_dice, stats.damage_type, stats.attack_bonus, stats.damage_bonus, stats.properties::jsonb
FROM torven_context ctx
CROSS JOIN (
    VALUES
        ('longsword', '1d8', 'рубящий', 5, 3, '["versatile"]'),
        ('dagger', '1d4', 'колющий', 5, 3, '["finesse","light","thrown"]')
) AS stats(code, damage_dice, damage_type, attack_bonus, damage_bonus, properties)
JOIN torven_items ti ON ti.code = stats.code;

INSERT INTO game.item_armor_stats (game_state_id, item_id, armor_class, armor_class_bonus, armor_type, stealth_disadvantage, strength_requirement)
SELECT ctx.game_state_id, ti.item_id, stats.armor_class, stats.armor_class_bonus, stats.armor_type, stats.stealth_disadvantage, stats.strength_requirement
FROM torven_context ctx
CROSS JOIN (
    VALUES
        ('shield', 0, 2, 'shield', false, NULL::integer),
        ('chainmail', 16, 0, 'heavy', true, 13)
) AS stats(code, armor_class, armor_class_bonus, armor_type, stealth_disadvantage, strength_requirement)
JOIN torven_items ti ON ti.code = stats.code;

INSERT INTO game.item_consumable_stats (game_state_id, item_id, uses, effects)
SELECT ctx.game_state_id, ti.item_id, 1, '[{"type":"food"}]'::jsonb
FROM torven_context ctx
JOIN torven_items ti ON ti.code = 'rations';

UPDATE game.equipped_gear eg
SET body_item_id = (SELECT item_id FROM torven_items WHERE code = 'chainmail'),
    main_hand_item_id = (SELECT item_id FROM torven_items WHERE code = 'longsword'),
    off_hand_item_id = (SELECT item_id FROM torven_items WHERE code = 'shield')
FROM torven_context ctx
WHERE eg.player_id = ctx.player_id;

UPDATE game.combat_stats cs
SET armor_class = 19,
    proficiency_bonus = 2
FROM torven_context ctx
WHERE cs.player_id = ctx.player_id;

INSERT INTO game.attacks (game_state_id, player_id, item_id, name, roll, damage, damage_type)
SELECT ctx.game_state_id, ctx.player_id, ti.item_id, atk.name, atk.roll, atk.damage, atk.damage_type
FROM torven_context ctx
CROSS JOIN (
    VALUES
        ('longsword', 'Длинный меч', '+5', '1d8+3 рубящий', 'рубящий'),
        ('dagger', 'Кинжал', '+5', '1d4+3 колющий', 'колющий')
) AS atk(code, name, roll, damage, damage_type)
JOIN torven_items ti ON ti.code = atk.code;

INSERT INTO game.game_log_entries (game_state_id, turn_number, type, text, important)
SELECT game_state_id, 0, 'system', 'Загружен пример персонажа Торвен Сталегрив.', true
FROM torven_context;

SELECT
    'Torven seed created' AS message,
    ctx.account_id,
    ctx.game_state_id,
    ctx.player_id,
    ctx.location_id,
    ctx.well_id
FROM torven_context ctx;

COMMIT;
