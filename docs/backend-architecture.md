# Backend Architecture

Backend is organized as a modular monolith. This keeps one deployable ASP.NET Core application and one PostgreSQL database, while separating feature ownership enough to grow the RPG MVP without turning controllers, services, and repositories into one large shared layer.

This is intentionally not microservices. The project still needs fast local iteration, simple transactions, shared auth, and one gameplay loop. A separate AI worker can be introduced later behind interfaces without splitting the whole backend now.

## Current Shape

The public API is still served by the same ASP.NET Core app. Existing routes are preserved.

Active module folders:
- `Modules/Play`: player-facing orchestration, play state, bootstrap, continue, safe changes.
- `Modules/Travel`: travel options and movement.
- `Modules/Combat`: combat controller/service/contracts/rules/repository.
- `Modules/Changes`: game change policy, dispatcher, handlers, service, repository.

Existing modules that have not yet been physically moved still live under `Controllers`, `Services`, `Repositories`, and `Contracts`. New gameplay work should go under `Modules`.

Shared cross-module code is being prepared under `Shared`. Existing shared result/error/database helpers are still in their old namespaces for compatibility and will move gradually.

## Ownership Rules

- Controllers call services, not repositories.
- `Play` is allowed to orchestrate other module services.
- `Play` must not directly call another module repository.
- Repositories own raw Npgsql SQL.
- Public routes must not be removed or renamed during module moves.
- `/api/players` remains absent.

## Play Flow

`POST /play/act` is the main player action endpoint.

1. It checks for pending mechanic requests.
2. If a roll is pending, it returns `PlayStateResponse` with `mode = awaiting_roll` and does not create a turn.
3. Otherwise it delegates turn creation to `ITurnService`.
4. It can apply supported safe changes through `IGameChangeService`.
5. It returns a fresh play state.

`resolve-and-continue` resolves mechanics through backend-owned mechanics services. Client roll/modifier values are not authoritative.

## Changes Dispatcher

`GameChangeOperationPolicy` normalizes Russian aliases and English canonical operation names.

`GameChangeDispatcher` now owns:
- unknown operation handling;
- unsupported operation handling;
- handler lookup;
- handler invocation.

The current handler layer is transitional: handlers delegate to the existing SQL implementation in `GameChangeRepository`. This keeps behavior stable while preparing the codebase to move each operation into a dedicated handler.

## Future AI Worker Boundary

The AI implementation still runs inside backend through `IOllamaClient`, `IPromptBuilder`, and `TurnService`. The long-term boundary is:
- `IAiClient`
- `IAiTurnProcessor`
- `IPromptBuilder`
- `IAiResponseValidator`

This lets a future AI worker consume the same prompt/validation contracts without rewriting Play.
