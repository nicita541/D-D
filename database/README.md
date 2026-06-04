# D&D PostgreSQL database

Dockerized PostgreSQL database for the RPG `GameState` system.

This project contains only the database layer. Backend API, JWT, C# models, controllers, and game engine logic are handled outside this folder.

## Start

From project root `E:\D&D`:

```powershell
docker compose up -d --build postgres migrations
```

Connection settings:

- Host: `localhost`
- Port: `5432`
- Database: `dnd`
- User: `dnd_user`
- Password: `dnd_password`

Connection string:

```text
Host=localhost;Port=5432;Database=dnd;Username=dnd_user;Password=dnd_password
```

## Migrations

PostgreSQL runs `init/` automatically only when `postgres_data` is empty. On every Compose startup, the `migrations` service applies missing scripts and records them in `infra.schema_migrations`.

- `001_create_schema.sql` is the base schema for a new volume and is never replayed on existing saves.
- Later scripts must be idempotent and preserve existing user data.
- `013_repair_bootstrap_text.sql` repairs only exact known corrupted bootstrap values.
- Do not use `docker compose down -v` during normal upgrades because it deletes saved campaigns.

## Schemas

- `auth` - account storage and refresh token storage.
- `game` - save games, player state, world state, items, quests, turns, and AI change proposals.

Passwords and refresh tokens are stored only as hashes:

- `auth.accounts.password_hash`
- `auth.refresh_tokens.token_hash`

## Auth Schema

- `auth.accounts` stores users.
- `auth.accounts.password_hash` stores only the password hash.
- `auth.accounts.role` is used by the backend as the JWT role claim. Allowed values are `user` and `admin`; default is `user`.
- `auth.refresh_tokens` stores long-lived refresh token records.
- `auth.refresh_tokens.token_hash` stores only the refresh token hash.
- Plain refresh tokens are never stored in the database.
- JWT access tokens are never stored in the database.
- Access tokens are short-lived; refresh tokens are long-lived and can be revoked or replaced.
- The backend returns tokens in JSON responses.

## Main Tables

Auth:

- `auth.accounts` - user accounts.
- `auth.refresh_tokens` - hashed refresh tokens.

Game root:

- `game.game_states` - save-game root. Every game table is scoped by `game_state_id`.
- `game.players` - player character for a save. `account_id` is protected by a composite FK to `game.game_states(id, account_id)`.

Player state:

- `game.player_progression`
- `game.player_resources`
- `game.conditions`
- `game.limited_resources`
- `game.player_attributes`
- `game.player_proficiencies`
- `game.abilities`
- `game.wealth`
- `game.player_needs`

Items and equipment:

- `game.item_instances`
- `game.item_weapon_stats`
- `game.item_armor_stats`
- `game.item_consumable_stats`
- `game.equipped_gear`

Combat:

- `game.combat_stats`
- `game.attacks`

World:

- `game.locations`
- `game.location_exits`
- `game.world_objects`
- `game.world_containers`
- `game.npcs`
- `game.factions`
- `game.monsters`

Quests and history:

- `game.quests`
- `game.quest_steps`
- `game.quest_reward_items`
- `game.game_log_entries`
- `game.game_turns`
- `game.game_changes`

## RPG Extension Tables

These tables support the newer backend endpoints for campaigns, story state, parties, combat, and AI context:

- `game.campaign_templates` - шаблоны сюжетов/кампаний: genre, tone, opening scene, main goal, master secrets, initial flags.
- `game.story_states` - текущий прогресс сюжета в сохранении: act, scene, goal, tension, plot flags, known/hidden facts, short memory.
- `game.parties` - партия игроков в одном сохранении.
- `game.party_members` - участники партии: host, player, observer, gm.
- `game.combat_states` - состояние боя в сохранении: active flag, round number, current turn participant.
- `game.combat_participants` - участники боя: character, npc, monster, initiative, HP, acted flag, local combat conditions.

These tables are added by `init/003_add_rpg_story_party_combat.sql`. The script is idempotent and uses the existing `game.set_updated_at()` trigger function.

`init/007_prepare_next_backend_stages.sql` adds the next backend-stage database support: AI turn processing fields, game change audit fields, monster/enemy actors, extra indexes, and monster data in `game.game_state_documents`.

## Create New Game

Create an account first. In real usage this should come from the backend registration flow, which must provide a real password hash.

```sql
INSERT INTO auth.accounts (email, username, password_hash, display_name)
VALUES ('player@example.com', 'player', 'fake_hash_for_local_dev_only', 'Player')
RETURNING id;
```

Create a new save:

```sql
SELECT game.create_new_game('<account-id>', 'Новая игра') AS game_state_id;
```

The function creates:

- `game.game_states`
- starter location
- current location link
- starter `game.game_log_entries` row

It does not create a default character. Character creation and character-domain rows are handled by the backend Character Domain API.

## GameState JSON View

The main document view is:

```sql
SELECT jsonb_pretty(data)
FROM game.game_state_documents
WHERE game_state_id = '<game-state-id>';
```

The view returns a JSON document with Russian field names:

- `персонажи`
- `персонаж`
- `прогресс`
- `ресурсы`
- `характеристики`
- `владения`
- `способности`
- `потребности`
- `богатство`
- `инвентарь`
- `экипировка`
- `бой`
- `мир`
- `локации`
- `нпс`
- `фракции`
- `монстры`
- `квесты`
- `история`
- `партия`

`game.player_documents` is no longer created. `game.game_state_documents` is the primary view.

## Next Backend Stages Readiness

The database is prepared for these backend endpoint groups:

- Character Domain API: progression, resources, attributes, wealth, needs, conditions, limited resources, proficiencies, abilities, items, equipment, combat stats, and attacks.
- Turns + AI loop: `game.game_turns` stores player message, master answer, raw AI response, status, model, error text, and completion timestamp.
- Apply/Reject changes: `game.game_changes` stores pending/applied/rejected proposals, reject reason, processing audit, error text, and structured apply result.
- World Domain API: locations, exits, world objects, containers, NPCs, factions, quests, quest steps, and reward items.
- Monster combat support: `game.monsters` stores monster actors, and `game.combat_participants.actor_type = 'monster'` can point to those rows.

## Backend Validation Rules

Some cross-table rules are intentionally left to backend validation because enforcing them with nullable polymorphic FKs would make item movement and `ON DELETE SET NULL` behavior fragile:

- An item must belong to a character inventory before it can be equipped.
- `game.attacks.item_id` must belong to the same character/game_state as the attack.
- `game.abilities.cost_resource_id` must belong to the same character/game_state as the ability.
- `game.quest_reward_items.item_id` must belong to the same game_state as the quest.
- `game.combat_participants.actor_type = 'monster'` must use an `actor_id` from `game.monsters`.
- Apply/reject endpoints must check `game_state` ownership through `game.game_states.account_id`.

## Migration Rules

- Add new schema changes as the next numbered file in `database/init`.
- Never modify arbitrary user or AI text in repair migrations.
- Make repair predicates exact and safe to run more than once.
- Verify a new migration both on an empty database and on an existing `postgres_data` volume.

## Why Items Use owner_kind/owner_id

Items are stored in one universal table: `game.item_instances`.

The columns `owner_kind` and `owner_id` describe where an item currently is:

- `owner_kind = 'player_inventory'`, `owner_id = player_id` means the item is in a player's inventory.
- `owner_kind = 'location'`, `owner_id = location_id` means the item lies in a location.
- `owner_kind = 'world_object'`, `owner_id = world_object_id` means the item is inside or attached to a world object, for example a well.
- `owner_kind = 'container'`, `owner_id = container_id` means the item is in a container.
- `owner_kind = 'npc'`, `owner_id = npc_id` means the item belongs to an NPC.

This avoids separate tables such as `player_inventory_items`, `location_items`, `container_items`, and `npc_items`. Moving an item is a single update to `owner_kind` and `owner_id`. Equipment still stores direct item UUID references in `game.equipped_gear`, so equipped slots point to real item instances, not item names.

## Torven Example

The Torven seed is not executed automatically on database startup. It is an optional local example:

```powershell
cd E:\D&D
docker cp .\database\examples\torven_seed.sql dnd-postgres:/tmp/torven_seed.sql
docker compose exec -T postgres psql -U dnd_user -d dnd -f /tmp/torven_seed.sql
```

It creates:

- fake test account
- new save via `game.create_new_game`
- Торвен Сталегрив
- stats, 12 HP, 25 gold
- long sword, shield, chainmail, dagger, rations, torches, waterskin
- starter location
- `Старый колодец`
- equipment linked through item UUIDs

## Smoke Test

Run after the database is up:

```powershell
cd E:\D&D
docker cp .\database\tests\smoke_test.sql dnd-postgres:/tmp/smoke_test.sql
docker compose exec -T postgres psql -U dnd_user -d dnd -f /tmp/smoke_test.sql
```

The smoke test runs inside a transaction and rolls back at the end. It prints explicit `OK NN` checks and raises `FAIL NN` errors with clear messages when something breaks.

It verifies:

- account creation
- default account role `user`
- refresh token hash storage
- refresh token hash uniqueness
- refresh token revocation metadata
- `game.create_new_game`
- `game.game_states.account_id`
- explicit test character domain rows
- sword item creation
- world object well creation
- equip sword in `main_hand_item_id`
- move sword to the well via `owner_kind = 'world_object'`
- clear `main_hand_item_id`
- valid JSON from `game.game_state_documents`
- `game.campaign_templates`
- `game.story_states`
- `game.parties`
- `game.party_members`
- `game.combat_states`
- `game.combat_participants`
- `game.monsters`
- `game.game_turns` AI-loop fields
- `game.game_changes` apply/reject audit fields
