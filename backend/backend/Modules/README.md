# Backend Modules

This backend is being organized as a modular monolith. Public HTTP routes remain stable while implementation files move behind module boundaries.

Current active modules in this refactor:
- `Play`: play-state orchestration and player-facing flow.
- `Travel`: location options and movement.
- `Combat`: combat endpoints, services, rules, and persistence.
- `Changes`: game change policy, dispatcher, handlers, service, and persistence.

Planned module folders may contain documentation or interfaces before they contain gameplay logic. They are not fake features.
