# Frontend API Contract

The detailed frontend contract currently lives in [frontend-api.md](frontend-api.md).

This file exists as the stable architecture-facing entry point for frontend/backend API ownership.

Rules:
- Existing public routes must remain compatible.
- Gameplay request bodies should prefer the documented Russian JSON fields.
- Existing English aliases should remain accepted where they already exist.
- `/api/players` must not return.

Key playable-flow routes:
- `GET /api/game-states/{gameStateId}/play/status`
- `POST /api/game-states/{gameStateId}/play/act`
- `POST /api/game-states/{gameStateId}/play/resolve-and-continue/{requestId}`
- `POST /api/game-states/{gameStateId}/play/apply-safe-changes`
- `GET /api/game-states/{gameStateId}/travel/options`
- `POST /api/game-states/{gameStateId}/play/travel`
- `POST /api/game-states/{gameStateId}/play/combat/start`
- `POST /api/game-states/{gameStateId}/play/combat/action`
- `POST /api/game-states/{gameStateId}/play/combat/end`

Update `frontend-api.md` first when payload shape changes.
