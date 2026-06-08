ALTER TABLE game.game_turns
    ADD COLUMN IF NOT EXISTS visible_player_message text;

ALTER TABLE game.game_turns
    ADD COLUMN IF NOT EXISTS turn_source text NOT NULL DEFAULT 'player';

ALTER TABLE game.game_turns
    ALTER COLUMN turn_source SET DEFAULT 'player';

UPDATE game.game_turns
SET turn_source = 'player'
WHERE turn_source IS NULL;

ALTER TABLE game.game_turns
    ALTER COLUMN turn_source SET NOT NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'game_turns_turn_source_check'
          AND conrelid = 'game.game_turns'::regclass
    ) THEN
        ALTER TABLE game.game_turns
            ADD CONSTRAINT game_turns_turn_source_check
            CHECK (turn_source IN ('player', 'system'));
    END IF;
END $$;

UPDATE game.game_turns
SET visible_player_message = player_message
WHERE visible_player_message IS NULL
  AND turn_source = 'player';

UPDATE game.game_turns
SET visible_player_message = NULL,
    turn_source = 'system'
WHERE player_message LIKE 'Начни новую одиночную RPG-сцену для моего персонажа.%'
   OR player_message LIKE 'Продолжи сцену после последнего результата проверки или механического действия.%'
   OR player_message LIKE 'Продолжи активный бой.%';

COMMENT ON COLUMN game.game_turns.player_message IS 'Internal/audit player instruction sent into the AI prompt.';
COMMENT ON COLUMN game.game_turns.visible_player_message IS 'Optional player text visible in gameplay chat. NULL means the turn is system/internal.';
COMMENT ON COLUMN game.game_turns.turn_source IS 'Turn source for UI filtering: player or system.';
