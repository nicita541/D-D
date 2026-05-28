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
        'текущаялокацияId', gs.current_location_id,

        'персонажи', COALESCE((
            SELECT jsonb_agg(
                jsonb_build_object(
                    'id', p.id,
                    'gameStateId', p.game_state_id,

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
                        'уровень', COALESCE(pp.level, 1),
                        'опыт', COALESCE(pp.experience, 0),
                        'опытДоСледующегоУровня', COALESCE(pp.experience_to_next_level, 300)
                    ),

                    'ресурсы', jsonb_build_object(
                        'хп', jsonb_build_object(
                            'максимум', COALESCE(pr.hp_max, 1),
                            'текущее', COALESCE(pr.hp_current, 1)
                        ),
                        'мана', jsonb_build_object(
                            'максимум', COALESCE(pr.mana_max, 0),
                            'текущее', COALESCE(pr.mana_current, 0)
                        ),
                        'очкиДействий', jsonb_build_object(
                            'максимум', COALESCE(pr.action_points_max, 1),
                            'текущее', COALESCE(pr.action_points_current, 1)
                        ),
                        'спасброскиСмерти', jsonb_build_object(
                            'активны', COALESCE(pr.death_saves_active, false),
                            'успехи', COALESCE(pr.death_saves_successes, 0),
                            'провалы', COALESCE(pr.death_saves_failures, 0)
                        ),
                        'состояния', COALESCE((
                            SELECT jsonb_agg(jsonb_build_object(
                                'id', c.id,
                                'название', c.name,
                                'тип', c.type,
                                'описание', c.description,
                                'источник', c.source,
                                'осталосьХодов', c.remaining_turns,
                                'постоянное', c.is_permanent,
                                'стаки', c.stacks,
                                'максимумСтаков', c.max_stacks,
                                'эффекты', c.effects,
                                'теги', c.tags
                            ) ORDER BY c.created_at, c.id)
                            FROM game.conditions c
                            WHERE c.player_id = p.id
                        ), '[]'::jsonb),
                        'ресурсыСпособностей', COALESCE((
                            SELECT jsonb_agg(jsonb_build_object(
                                'id', lr.id,
                                'название', lr.name,
                                'максимум', lr.max_value,
                                'текущее', lr.current_value,
                                'восстановление', lr.recovery
                            ) ORDER BY lr.name, lr.id)
                            FROM game.limited_resources lr
                            WHERE lr.player_id = p.id
                        ), '[]'::jsonb)
                    ),

                    'характеристики', jsonb_build_object(
                        'сила', COALESCE(pa.strength, 10),
                        'ловкость', COALESCE(pa.dexterity, 10),
                        'телосложение', COALESCE(pa.constitution, 10),
                        'интеллект', COALESCE(pa.intelligence, 10),
                        'мудрость', COALESCE(pa.wisdom, 10),
                        'харизма', COALESCE(pa.charisma, 10),
                        'инициатива', COALESCE(pa.initiative, 0),
                        'скорость', COALESCE(pa.speed, 9),
                        'восприятие', COALESCE(pa.perception, 10)
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
                        'еда', jsonb_build_object(
                            'размер', COALESCE(pn.food_size, 'средний'),
                            'вдень', pn.food_per_day,
                            'остаток', COALESCE(pn.food_remaining, 0)
                        ),
                        'вода', jsonb_build_object(
                            'размер', COALESCE(pn.water_size, 'средний'),
                            'вдень', pn.water_per_day,
                            'остаток', COALESCE(pn.water_remaining, 0)
                        ),
                        'грузоподъемность', jsonb_build_object(
                            'максимум', COALESCE(pn.carry_capacity_max, 0),
                            'текущийВес', COALESCE(pn.carry_current_weight, 0),
                            'единица', COALESCE(pn.carry_unit, 'кг')
                        ),
                        'передвижение', jsonb_build_object(
                            'метровЗаХод', COALESCE(pn.movement_meters_per_turn, 9),
                            'кмВДень', COALESCE(pn.movement_km_per_day, 36)
                        )
                    ),

                    'богатство', jsonb_build_object(
                        'монеты', jsonb_build_object(
                            'медные', COALESCE(w.copper, 0),
                            'серебряные', COALESCE(w.silver, 0),
                            'золотые', COALESCE(w.gold, 0),
                            'платиновые', COALESCE(w.platinum, 0)
                        )
                    ),

                    'инвентарь', COALESCE((
                        SELECT jsonb_agg(jsonb_build_object(
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
                            'теги', item.tags
                        ) ORDER BY item.name, item.id)
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
                        'основнаяРука', eg.main_hand_item_id,
                        'втораяРука', eg.off_hand_item_id,
                        'амулет', eg.amulet_item_id,
                        'кольцо1', eg.ring1_item_id,
                        'кольцо2', eg.ring2_item_id
                    ),

                    'бой', jsonb_build_object(
                        'классДоспеха', COALESCE(cs.armor_class, 10),
                        'бонусМастерства', COALESCE(cs.proficiency_bonus, 2),
                        'вБою', COALESCE(cs.in_combat, false),
                        'бросокИнициативы', COALESCE(cs.initiative_roll, 0),
                        'атаки', COALESCE((
                            SELECT jsonb_agg(jsonb_build_object(
                                'id', a.id,
                                'название', a.name,
                                'бросок', a.roll,
                                'урон', a.damage,
                                'типУрона', a.damage_type,
                                'предметId', a.item_id
                            ) ORDER BY a.name, a.id)
                            FROM game.attacks a
                            WHERE a.player_id = p.id
                        ), '[]'::jsonb)
                    )
                )
                ORDER BY p.created_at, p.id
            )
            FROM game.players p
            LEFT JOIN game.player_progression pp ON pp.player_id = p.id
            LEFT JOIN game.player_resources pr ON pr.player_id = p.id
            LEFT JOIN game.player_attributes pa ON pa.player_id = p.id
            LEFT JOIN game.wealth w ON w.player_id = p.id
            LEFT JOIN game.player_needs pn ON pn.player_id = p.id
            LEFT JOIN game.equipped_gear eg ON eg.player_id = p.id
            LEFT JOIN game.combat_stats cs ON cs.player_id = p.id
            WHERE p.game_state_id = gs.id
        ), '[]'::jsonb),

        'партия', COALESCE((
            SELECT jsonb_build_object(
                'id', party.id,
                'название', party.name,
                'участники', COALESCE((
                    SELECT jsonb_agg(jsonb_build_object(
                        'id', pm.id,
                        'accountId', pm.account_id,
                        'characterId', pm.character_id,
                        'роль', pm.role,
                        'статус', pm.status,
                        'отображаемоеИмя', pm.display_name
                    ) ORDER BY pm.created_at, pm.id)
                    FROM game.party_members pm
                    WHERE pm.party_id = party.id
                ), '[]'::jsonb)
            )
            FROM game.parties party
            WHERE party.game_state_id = gs.id
            LIMIT 1
        ), '{}'::jsonb),

        'мир', jsonb_build_object(
            'текущаялокацияId', gs.current_location_id,
            'локации', COALESCE((
                SELECT jsonb_object_agg(
                    loc.id::text,
                    jsonb_build_object(
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
                    )
                    ORDER BY loc.name, loc.id
                )
                FROM game.locations loc
                WHERE loc.game_state_id = gs.id
            ), '{}'::jsonb),

            'нпс', COALESCE((
                SELECT jsonb_object_agg(
                    npc.id::text,
                    jsonb_build_object(
                        'id', npc.id,
                        'локацияId', npc.location_id,
                        'имя', npc.name,
                        'роль', npc.role,
                        'отношение', npc.attitude,
                        'описание', npc.description,
                        'жив', npc.is_alive
                    )
                    ORDER BY npc.name, npc.id
                )
                FROM game.npcs npc
                WHERE npc.game_state_id = gs.id
            ), '{}'::jsonb),

            'фракции', COALESCE((
                SELECT jsonb_object_agg(
                    f.id::text,
                    jsonb_build_object(
                        'id', f.id,
                        'название', f.name,
                        'репутация', f.reputation,
                        'описание', f.description
                    )
                    ORDER BY f.name, f.id
                )
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
                'предметыНаграды', COALESCE((
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
FROM game.game_states gs;