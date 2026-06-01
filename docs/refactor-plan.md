# Backend Refactor Plan

## Stage A: Modular Skeleton Without Behavior Change

Status: completed for active modules.

- Create `Modules` and `Shared`.
- Move Play, Travel, Combat, and Changes implementation files under module folders.
- Bring active module namespaces to `backend.Modules.Play`, `backend.Modules.Travel`, `backend.Modules.Combat`, and `backend.Modules.Changes`.
- Preserve all public routes.
- Keep existing tests and smoke scripts green.

## Stage B: Game Change Handlers

Status: transitional implementation added.

- Add `IGameChangeHandler`.
- Add `GameChangeContext`.
- Add `GameChangeDispatcher`.
- Register handlers in DI.
- Normalize operations through `GameChangeOperationPolicy`.
- Remove duplicate Russian switch handling from repository switch.

Current limitation: handlers delegate back into `GameChangeRepository` for SQL execution. This keeps behavior stable. The next refactor should move SQL from repository switch into handlers or operation-specific repositories.

Additional cleanup completed:
- `RepositoryBackedChangeHandlerBase` introduced to remove repeated handler boilerplate.
- `GameChangeDispatcher` fails fast for duplicate handlers.
- `GameChangeOperationPolicy` exposes `IsKnown`, `IsSupported`, `IsSafeAutoApply`, and `TryCanonicalize`.

## Stage C: Finish Current Closed-Loop Module Split

Status: completed for Play, Travel, Combat, and Changes.

- Play files now live under `Modules/Play`.
- Travel files now live under `Modules/Travel`.
- Combat files now live under `Modules/Combat`.
- Changes files now live under `Modules/Changes`.

Remaining work:
- Move additional modules gradually: Auth, GameStates, Characters, Turns, Ai, Mechanics, Memory, World, Inventory.
- Reduce legacy `Controllers/Services/Repositories` folders as modules migrate.
- Move SQL out of `GameChangeRepository` into operation handlers in a later low-risk tranche.

## Stage D: Prepare Planned Modules

Status: documented.

Planned modules should receive interfaces, docs, and contracts before gameplay code. Do not add fake behavior.

## Stage E: Gameplay Tranches

Start only after module boundaries and handler architecture are stable.

Each tranche must run:
- `dotnet build`
- `dotnet test Tests\Tests.csproj`
- `docker compose down -v --remove-orphans`
- `docker compose up -d --build postgres backend llm`
- `scripts/api-critical-smoke.ps1`
- `scripts/full-game-smoke.ps1`
- `scripts/closed-loop-game-smoke.ps1`
