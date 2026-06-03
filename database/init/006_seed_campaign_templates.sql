INSERT INTO game.campaign_templates
(
    title,
    genre,
    tone,
    summary,
    opening_scene,
    main_goal,
    master_secrets,
    initial_flags
)
SELECT
    'Проклятие старой дороги',
    'dark fantasy',
    'мрачный, тревожный, мистический',
    'Герои прибывают в пограничные земли, где торговая дорога опустела после серии исчезновений.',
    'Вечером герои входят в придорожную деревню Вороньи Холмы. В трактире тихо, местные избегают смотреть в сторону старой дороги.',
    'Выяснить причину исчезновений на старой дороге и снять проклятие.',
    '[
        "Исчезновения связаны не с бандитами, а с древним договором между деревней и лесным духом.",
        "Староста скрывает, что его семья много лет приносила жертвы духу.",
        "Проклятие можно снять без боя, если восстановить нарушенный обет."
    ]'::jsonb,
    '{
        "startingLocation": "Вороньи Холмы",
        "dangerLevel": 2,
        "mainThreat": "forest_spirit",
        "allowDiplomaticResolution": true
    }'::jsonb
WHERE NOT EXISTS (
    SELECT 1 FROM game.campaign_templates WHERE title = 'Проклятие старой дороги'
);

INSERT INTO game.campaign_templates
(
    title,
    genre,
    tone,
    summary,
    opening_scene,
    main_goal,
    master_secrets,
    initial_flags
)
SELECT
    'Осколок павшей звезды',
    'adventure fantasy',
    'героический, загадочный, опасный',
    'После падения небесного осколка в горах начали пробуждаться древние механизмы и странная магия.',
    'Герои видят ночной след падающей звезды. Утром над дальними горами поднимается голубой свет.',
    'Найти осколок звезды раньше охотников за артефактами и понять его природу.',
    '[
        "Осколок является частью древнего небесного устройства.",
        "Один из союзников нанимателя работает на конкурирующую фракцию.",
        "Артефакт может исцелять землю, но при неправильном использовании открывает портал."
    ]'::jsonb,
    '{
        "startingLocation": "Горный тракт",
        "dangerLevel": 3,
        "mainThreat": "artifact_hunters",
        "artifactIsUnstable": true
    }'::jsonb
WHERE NOT EXISTS (
    SELECT 1 FROM game.campaign_templates WHERE title = 'Осколок павшей звезды'
);

INSERT INTO game.campaign_templates
(
    title,
    genre,
    tone,
    summary,
    opening_scene,
    main_goal,
    master_secrets,
    initial_flags
)
SELECT
    'Подземелье под Чёрной башней',
    'classic dungeon crawl',
    'опасный, исследовательский, напряжённый',
    'Под руинами старой башни открывается вход в забытое подземелье, где лежит источник магической порчи.',
    'Герои стоят у провала в каменном полу Чёрной башни. Из глубины тянет холодом и слышен далёкий металлический стук.',
    'Исследовать подземелье, найти источник порчи и выбраться живыми.',
    '[
        "Подземелье построено вокруг древнего запечатанного ядра.",
        "Не все монстры внизу враждебны.",
        "Часть ловушек можно отключить через старый механизм на втором уровне."
    ]'::jsonb,
    '{
        "startingLocation": "Чёрная башня",
        "dangerLevel": 4,
        "mainThreat": "corrupted_core",
        "dungeonMode": true
    }'::jsonb
WHERE NOT EXISTS (
    SELECT 1 FROM game.campaign_templates WHERE title = 'Подземелье под Чёрной башней'
);