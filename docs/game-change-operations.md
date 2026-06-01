# Game Change Operations

Game changes use canonical English `snake_case` names internally. Russian operation names are accepted as aliases through `GameChangeOperationPolicy`.

## Classification

- `safe`: can be auto-applied when supported.
- `dangerous`: recognized but never auto-applied.
- `unsupported`: recognized as planned/hook, but not implemented.
- `unknown`: not recognized and returns a controlled validation error.

Unknown and unsupported operations must not produce `500`.

## Supported Safe Operations

- `add_journal_entry`
- `update_memory`
- `update_scene`
- `create_quest`
- `update_quest`
- `create_quest_step`
- `complete_quest_step`
- `create_location`
- `update_location`
- `create_npc`
- `update_npc`
- `create_world_object`
- `update_world_object`
- `add_item`

## Supported Manual/Dangerous Operations

These have backend support but are not auto-applied:

- `request_roll`
- `change_hp`
- `change_resource`
- `add_condition`
- `delete_condition`
- `move_item`
- `move_party_to_location`
- `set_current_location`
- `open_location_exit`
- `close_location_exit`
- `lock_location_exit`
- `unlock_location_exit`

## Recognized Hooks Not Yet Implemented

Examples:
- `spawn_monster`
- `create_monster`
- `kill_monster`
- `start_combat`
- `end_combat`
- `add_combat_participant`
- `remove_combat_participant`
- `add_xp`
- `level_up`
- `short_rest`
- `long_rest`

These remain visible as pending/skipped changes with a reason such as `unsupported operation` or `dangerous operation`.

## Dispatcher

`GameChangeDispatcher` performs normalization and handler lookup. Current handlers delegate to the existing repository SQL executor. Future work should move each operation's SQL into its handler or a small operation-specific repository.
