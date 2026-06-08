# Snapshots Module

Stores host-controlled rollback points for a single `GameState`.

Surface:

- `GET /api/game-states/{gameStateId}/snapshots`
- `POST /api/game-states/{gameStateId}/snapshots`
- `POST /api/game-states/{gameStateId}/snapshots/{snapshotId}/restore`

Access:

- party members can list snapshots;
- only host/manage access can create or restore snapshots.

The snapshot payload stores game-scoped RPG tables only. Collaboration metadata such as party members, invite tokens and invite audit rows is intentionally not restored.
