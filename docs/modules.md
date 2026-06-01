# Backend Modules

## Active Modules

All active modules listed here use `backend.Modules.*` namespaces. Public routes remain unchanged.

### Play
Owns the player-facing orchestration surface:
- `play/status`
- `play/act`
- `play/start`
- `play/message`
- `play/continue`
- mechanic resolve and continue
- safe change application
- bootstrap
- play-combat wrappers

HTTP controller responsibilities are intentionally small: current user lookup, facade/application service call, and `RpgResult<T>` mapping. Orchestration belongs to `PlayApplicationService`, `PlayTravelFacade`, `PlayCombatFacade`, `PlayOrchestratorService`, and `PlayStateService`.

### Travel
Owns travel options and movement:
- validates target locations and exits;
- supports direct/manual movement when no `exitId` is supplied;
- updates current location and scene/log state.

### Combat
Owns base combat behavior:
- combat start/end;
- participants;
- next turn;
- attack;
- damage/heal.

Play-combat endpoints delegate into this module instead of duplicating combat logic.

### Changes
Owns game change operation policy and apply/reject flow:
- alias normalization;
- safe/dangerous/unsupported/unknown classification;
- dispatcher;
- repository-backed handlers;
- raw SQL operation application.

Handlers are currently repository-backed via `RepositoryBackedChangeHandlerBase`. They do not contain fake behavior and do not change SQL semantics.

## Existing Modules Still In Legacy Folders

These are functional but not yet physically moved into `Modules`:
- Auth
- GameStates
- Characters
- Turns
- Ai
- Mechanics
- Memory
- World
- Inventory
- Campaigns
- Progression

## Planned Module Folders

The 40-point MVP roadmap will add or expand:
- Economy
- Quests
- Notes
- Snapshots
- Time
- Rest
- Survival
- Conditions
- Dialogues
- WorldEvents
- ExportImport
- DevTools

Empty modules should not contain fake endpoints. Add interfaces or documentation first, then real behavior in a gameplay tranche.

This document describes architecture cleanup only. Monster, loot, XP, rewards, and additional gameplay systems are not implemented by this tranche.
