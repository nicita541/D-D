CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE OR REPLACE FUNCTION game.set_updated_at()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$;

CREATE TABLE IF NOT EXISTS game.campaign_templates (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    title text NOT NULL,
    genre text,
    tone text,
    summary text,
    opening_scene text,
    main_goal text,
    master_secrets jsonb NOT NULL DEFAULT '[]'::jsonb,
    initial_flags jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

COMMENT ON TABLE game.campaign_templates IS 'Reusable RPG campaign/story template: genre, tone, opening scene, goal, and master-only secrets.';
COMMENT ON COLUMN game.campaign_templates.master_secrets IS 'Hidden master-only facts for initial campaign context.';
COMMENT ON COLUMN game.campaign_templates.initial_flags IS 'Initial story flags copied or interpreted when a new story state starts.';

CREATE TABLE IF NOT EXISTS game.story_states (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL UNIQUE REFERENCES game.game_states(id) ON DELETE CASCADE,
    campaign_template_id uuid REFERENCES game.campaign_templates(id) ON DELETE SET NULL,
    current_act text,
    current_scene text,
    current_goal text,
    tension_level integer NOT NULL DEFAULT 1,
    plot_flags jsonb NOT NULL DEFAULT '{}'::jsonb,
    known_facts jsonb NOT NULL DEFAULT '[]'::jsonb,
    hidden_facts jsonb NOT NULL DEFAULT '[]'::jsonb,
    short_memory jsonb NOT NULL DEFAULT '[]'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT story_states_tension_level_check CHECK (tension_level >= 0)
);

COMMENT ON TABLE game.story_states IS 'Current story progress for one save game, optionally based on a campaign template.';
COMMENT ON COLUMN game.story_states.plot_flags IS 'Mutable story flags for backend and AI context.';
COMMENT ON COLUMN game.story_states.known_facts IS 'Facts known to the player party.';
COMMENT ON COLUMN game.story_states.hidden_facts IS 'Facts hidden from the player but available to the game master.';
COMMENT ON COLUMN game.story_states.short_memory IS 'Recent compact story memory for AI context.';

CREATE TABLE IF NOT EXISTS game.parties (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL UNIQUE REFERENCES game.game_states(id) ON DELETE CASCADE,
    name text NOT NULL DEFAULT 'Партия',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT parties_id_game_state_id_unique UNIQUE (id, game_state_id)
);

COMMENT ON TABLE game.parties IS 'Player party for a save game. The current model keeps one party per game_state.';

CREATE TABLE IF NOT EXISTS game.party_members (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL REFERENCES game.game_states(id) ON DELETE CASCADE,
    party_id uuid NOT NULL REFERENCES game.parties(id) ON DELETE CASCADE,
    account_id uuid REFERENCES auth.accounts(id) ON DELETE SET NULL,
    character_id uuid REFERENCES game.players(id) ON DELETE SET NULL,
    role text NOT NULL DEFAULT 'player',
    status text NOT NULL DEFAULT 'active',
    display_name text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT party_members_party_game_state_fkey FOREIGN KEY (party_id, game_state_id)
        REFERENCES game.parties(id, game_state_id) ON DELETE CASCADE,
    CONSTRAINT party_members_role_check CHECK (role IN ('host', 'player', 'observer', 'gm')),
    CONSTRAINT party_members_status_check CHECK (status IN ('active', 'inactive', 'left', 'kicked'))
);

COMMENT ON TABLE game.party_members IS 'Accounts, characters, observers, or GM entries that belong to a save-game party.';
COMMENT ON COLUMN game.party_members.role IS 'Party role: host, player, observer, or gm.';
COMMENT ON COLUMN game.party_members.status IS 'Membership status: active, inactive, left, or kicked.';

CREATE TABLE IF NOT EXISTS game.combat_states (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL UNIQUE REFERENCES game.game_states(id) ON DELETE CASCADE,
    is_active boolean NOT NULL DEFAULT false,
    round_number integer NOT NULL DEFAULT 1,
    current_turn_participant_id uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT combat_states_round_check CHECK (round_number >= 1),
    CONSTRAINT combat_states_id_game_state_id_unique UNIQUE (id, game_state_id)
);

COMMENT ON TABLE game.combat_states IS 'Current combat tracker state for one save game.';
COMMENT ON COLUMN game.combat_states.current_turn_participant_id IS 'Current combat participant whose turn is active.';

CREATE TABLE IF NOT EXISTS game.combat_participants (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL REFERENCES game.game_states(id) ON DELETE CASCADE,
    combat_state_id uuid NOT NULL REFERENCES game.combat_states(id) ON DELETE CASCADE,
    actor_type text NOT NULL,
    actor_id uuid NOT NULL,
    name text NOT NULL,
    initiative integer NOT NULL DEFAULT 0,
    hp_current integer NOT NULL DEFAULT 0,
    hp_max integer NOT NULL DEFAULT 0,
    has_acted boolean NOT NULL DEFAULT false,
    conditions jsonb NOT NULL DEFAULT '[]'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT combat_participants_combat_state_game_state_fkey FOREIGN KEY (combat_state_id, game_state_id)
        REFERENCES game.combat_states(id, game_state_id) ON DELETE CASCADE,
    CONSTRAINT combat_participants_actor_type_check CHECK (actor_type IN ('character', 'npc', 'monster')),
    CONSTRAINT combat_participants_hp_check CHECK (hp_current >= 0 AND hp_max >= 0)
);

COMMENT ON TABLE game.combat_participants IS 'Participants in the current combat: player characters, NPCs, or monsters.';
COMMENT ON COLUMN game.combat_participants.actor_type IS 'Actor type: character, npc, or monster.';
COMMENT ON COLUMN game.combat_participants.actor_id IS 'ID of the represented actor. The referenced table depends on actor_type and is validated by backend rules.';
COMMENT ON COLUMN game.combat_participants.conditions IS 'Combat-local condition snapshot for initiative tracker context.';

ALTER TABLE game.combat_states
DROP CONSTRAINT IF EXISTS combat_states_current_turn_participant_fk;

ALTER TABLE game.combat_states
ADD CONSTRAINT combat_states_current_turn_participant_fk
FOREIGN KEY (current_turn_participant_id)
REFERENCES game.combat_participants(id)
ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS ix_campaign_templates_title
    ON game.campaign_templates(title);

CREATE INDEX IF NOT EXISTS ix_story_states_game_state_id
    ON game.story_states(game_state_id);

CREATE INDEX IF NOT EXISTS ix_story_states_campaign_template_id
    ON game.story_states(campaign_template_id);

CREATE INDEX IF NOT EXISTS ix_parties_game_state_id
    ON game.parties(game_state_id);

CREATE INDEX IF NOT EXISTS ix_party_members_game_state_id
    ON game.party_members(game_state_id);

CREATE INDEX IF NOT EXISTS ix_party_members_party_id
    ON game.party_members(party_id);

CREATE INDEX IF NOT EXISTS ix_party_members_account_id
    ON game.party_members(account_id);

CREATE INDEX IF NOT EXISTS ix_party_members_character_id
    ON game.party_members(character_id);

CREATE INDEX IF NOT EXISTS ix_combat_states_game_state_id
    ON game.combat_states(game_state_id);

CREATE INDEX IF NOT EXISTS ix_combat_participants_game_state_id
    ON game.combat_participants(game_state_id);

CREATE INDEX IF NOT EXISTS ix_combat_participants_combat_state_id
    ON game.combat_participants(combat_state_id);

CREATE INDEX IF NOT EXISTS ix_combat_participants_actor
    ON game.combat_participants(actor_type, actor_id);

DROP TRIGGER IF EXISTS trg_campaign_templates_updated_at ON game.campaign_templates;
CREATE TRIGGER trg_campaign_templates_updated_at
BEFORE UPDATE ON game.campaign_templates
FOR EACH ROW
EXECUTE FUNCTION game.set_updated_at();

DROP TRIGGER IF EXISTS trg_story_states_updated_at ON game.story_states;
CREATE TRIGGER trg_story_states_updated_at
BEFORE UPDATE ON game.story_states
FOR EACH ROW
EXECUTE FUNCTION game.set_updated_at();

DROP TRIGGER IF EXISTS trg_parties_updated_at ON game.parties;
CREATE TRIGGER trg_parties_updated_at
BEFORE UPDATE ON game.parties
FOR EACH ROW
EXECUTE FUNCTION game.set_updated_at();

DROP TRIGGER IF EXISTS trg_party_members_updated_at ON game.party_members;
CREATE TRIGGER trg_party_members_updated_at
BEFORE UPDATE ON game.party_members
FOR EACH ROW
EXECUTE FUNCTION game.set_updated_at();

DROP TRIGGER IF EXISTS trg_combat_states_updated_at ON game.combat_states;
CREATE TRIGGER trg_combat_states_updated_at
BEFORE UPDATE ON game.combat_states
FOR EACH ROW
EXECUTE FUNCTION game.set_updated_at();

DROP TRIGGER IF EXISTS trg_combat_participants_updated_at ON game.combat_participants;
CREATE TRIGGER trg_combat_participants_updated_at
BEFORE UPDATE ON game.combat_participants
FOR EACH ROW
EXECUTE FUNCTION game.set_updated_at();
