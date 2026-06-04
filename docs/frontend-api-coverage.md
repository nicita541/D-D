# Frontend API coverage

Generated from the running backend Swagger document. Total routes: **172**.

The frontend exposes unique backend capabilities through player, owner-management, or admin interfaces. Compatibility aliases are intentionally not rendered as separate actions. Player mode never renders memory master secrets or management-only actions.

| # | Route | Interface | Coverage |
|---:|---|---|---|
| 1 | `DELETE api/campaigns/{id}` | admin | `/admin/campaigns` (role=admin) |
| 2 | `DELETE api/game-states/{gameStateId}` | games | `/games` и заголовок management |
| 3 | `DELETE api/game-states/{gameStateId}/characters/{characterId}` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 4 | `DELETE api/game-states/{gameStateId}/characters/{characterId}/abilities/{abilityId}` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 5 | `DELETE api/game-states/{gameStateId}/characters/{characterId}/attacks/{attackId}` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 6 | `DELETE api/game-states/{gameStateId}/characters/{characterId}/conditions/{conditionId}` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 7 | `DELETE api/game-states/{gameStateId}/characters/{characterId}/inventory/items/{itemId}` | alias-only | Не показывается отдельно; UI использует канонический маршрут |
| 8 | `DELETE api/game-states/{gameStateId}/characters/{characterId}/inventory/{itemId}` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 9 | `DELETE api/game-states/{gameStateId}/characters/{characterId}/limited-resources/{resourceId}` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 10 | `DELETE api/game-states/{gameStateId}/characters/{characterId}/proficiencies/{proficiencyId}` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 11 | `DELETE api/game-states/{gameStateId}/party/members/{memberId}` | management/story | `/games/:id/manage/story` |
| 12 | `DELETE api/game-states/{gameStateId}/world/containers/{containerId}` | management/world | `/games/:id/manage/world` |
| 13 | `DELETE api/game-states/{gameStateId}/world/factions/{factionId}` | management/world | `/games/:id/manage/world` |
| 14 | `DELETE api/game-states/{gameStateId}/world/locations/{locationId}` | management/world | `/games/:id/manage/world` |
| 15 | `DELETE api/game-states/{gameStateId}/world/locations/{locationId}/exits/{exitId}` | management/world | `/games/:id/manage/world` |
| 16 | `DELETE api/game-states/{gameStateId}/world/monsters/{monsterId}` | management/world | `/games/:id/manage/world` |
| 17 | `DELETE api/game-states/{gameStateId}/world/npcs/{npcId}` | management/world | `/games/:id/manage/world` |
| 18 | `DELETE api/game-states/{gameStateId}/world/objects/{objectId}` | management/world | `/games/:id/manage/world` |
| 19 | `DELETE api/game-states/{gameStateId}/world/quests/{questId}` | management/world | `/games/:id/manage/world` |
| 20 | `DELETE api/game-states/{gameStateId}/world/quests/{questId}/steps/{stepId}` | management/world | `/games/:id/manage/world` |
| 21 | `GET api/auth/me` | auth | `/login`, `/register`, AuthProvider и single-flight refresh |
| 22 | `GET api/campaigns` | games/admin | `/games` (выбор шаблона) и `/admin/campaigns` |
| 23 | `GET api/campaigns/{id}` | games/admin | `/games` (выбор шаблона) и `/admin/campaigns` |
| 24 | `GET api/game-states` | games | `/games` и заголовок management |
| 25 | `GET api/game-states/{gameStateId}` | games | `/games` и заголовок management |
| 26 | `GET api/game-states/{gameStateId}/ai-context` | management/history | `/games/:id/manage/history` |
| 27 | `GET api/game-states/{gameStateId}/changes` | management/history | `/games/:id/manage/history` |
| 28 | `GET api/game-states/{gameStateId}/changes/{changeId}` | management/history | `/games/:id/manage/history` |
| 29 | `GET api/game-states/{gameStateId}/characters` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 30 | `GET api/game-states/{gameStateId}/characters/{characterId}` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 31 | `GET api/game-states/{gameStateId}/characters/{characterId}/abilities` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 32 | `GET api/game-states/{gameStateId}/characters/{characterId}/attacks` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 33 | `GET api/game-states/{gameStateId}/characters/{characterId}/conditions` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 34 | `GET api/game-states/{gameStateId}/characters/{characterId}/currency` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 35 | `GET api/game-states/{gameStateId}/characters/{characterId}/equipment` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 36 | `GET api/game-states/{gameStateId}/characters/{characterId}/inventory` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 37 | `GET api/game-states/{gameStateId}/characters/{characterId}/limited-resources` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 38 | `GET api/game-states/{gameStateId}/characters/{characterId}/needs` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 39 | `GET api/game-states/{gameStateId}/characters/{characterId}/proficiencies` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 40 | `GET api/game-states/{gameStateId}/characters/{characterId}/progression` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 41 | `GET api/game-states/{gameStateId}/checks` | management/history | `/games/:id/manage/history` |
| 42 | `GET api/game-states/{gameStateId}/combat` | management/encounters | `/games/:id/manage/encounters` |
| 43 | `GET api/game-states/{gameStateId}/combat/outcome` | management/encounters | `/games/:id/manage/encounters` |
| 44 | `GET api/game-states/{gameStateId}/loot` | management/encounters | `/games/:id/manage/encounters` |
| 45 | `GET api/game-states/{gameStateId}/loot/{lootContainerId}` | management/encounters | `/games/:id/manage/encounters` |
| 46 | `GET api/game-states/{gameStateId}/mechanic-requests` | management/history | `/games/:id/manage/history` |
| 47 | `GET api/game-states/{gameStateId}/memory` | management/story | `/games/:id/manage/story` |
| 48 | `GET api/game-states/{gameStateId}/monsters` | alias-only | Не показывается отдельно; UI использует канонический маршрут |
| 49 | `GET api/game-states/{gameStateId}/monsters/{monsterId}` | alias-only | Не показывается отдельно; UI использует канонический маршрут |
| 50 | `GET api/game-states/{gameStateId}/party` | management/story | `/games/:id/manage/story` |
| 51 | `GET api/game-states/{gameStateId}/play/status` | game | `/games/:id/play` |
| 52 | `GET api/game-states/{gameStateId}/rolls` | management/history | `/games/:id/manage/history` |
| 53 | `GET api/game-states/{gameStateId}/story` | management/story | `/games/:id/manage/story` |
| 54 | `GET api/game-states/{gameStateId}/time` | game | `/games/:id/play` |
| 55 | `GET api/game-states/{gameStateId}/travel/options` | game | `/games/:id/play` |
| 56 | `GET api/game-states/{gameStateId}/turns` | management/history | `/games/:id/manage/history` |
| 57 | `GET api/game-states/{gameStateId}/turns/{turnId}` | management/history | `/games/:id/manage/history` |
| 58 | `GET api/game-states/{gameStateId}/world/containers` | management/world | `/games/:id/manage/world` |
| 59 | `GET api/game-states/{gameStateId}/world/factions` | management/world | `/games/:id/manage/world` |
| 60 | `GET api/game-states/{gameStateId}/world/locations` | management/world | `/games/:id/manage/world` |
| 61 | `GET api/game-states/{gameStateId}/world/locations/{locationId}` | management/world | `/games/:id/manage/world` |
| 62 | `GET api/game-states/{gameStateId}/world/locations/{locationId}/exits` | management/world | `/games/:id/manage/world` |
| 63 | `GET api/game-states/{gameStateId}/world/monsters` | management/world | `/games/:id/manage/world` |
| 64 | `GET api/game-states/{gameStateId}/world/monsters/{monsterId}` | management/world | `/games/:id/manage/world` |
| 65 | `GET api/game-states/{gameStateId}/world/npcs` | management/world | `/games/:id/manage/world` |
| 66 | `GET api/game-states/{gameStateId}/world/objects` | management/world | `/games/:id/manage/world` |
| 67 | `GET api/game-states/{gameStateId}/world/quests` | management/world | `/games/:id/manage/world` |
| 68 | `GET api/game-states/{gameStateId}/world/quests/{questId}/steps` | management/world | `/games/:id/manage/world` |
| 69 | `GET health` | runtime | Compose healthcheck и локальная диагностика |
| 70 | `GET health/db` | runtime | Compose healthcheck и локальная диагностика |
| 71 | `POST api/auth/login` | auth | `/login`, `/register`, AuthProvider и single-flight refresh |
| 72 | `POST api/auth/logout` | auth | `/login`, `/register`, AuthProvider и single-flight refresh |
| 73 | `POST api/auth/refresh` | auth | `/login`, `/register`, AuthProvider и single-flight refresh |
| 74 | `POST api/auth/register` | auth | `/login`, `/register`, AuthProvider и single-flight refresh |
| 75 | `POST api/campaigns` | admin | `/admin/campaigns` (role=admin) |
| 76 | `POST api/game-states` | games | `/games` и заголовок management |
| 77 | `POST api/game-states/{gameStateId}/changes/{changeId}/apply` | management/history | `/games/:id/manage/history` |
| 78 | `POST api/game-states/{gameStateId}/changes/{changeId}/reject` | management/history | `/games/:id/manage/history` |
| 79 | `POST api/game-states/{gameStateId}/characters` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 80 | `POST api/game-states/{gameStateId}/characters/{characterId}/abilities` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 81 | `POST api/game-states/{gameStateId}/characters/{characterId}/attacks` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 82 | `POST api/game-states/{gameStateId}/characters/{characterId}/conditions` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 83 | `POST api/game-states/{gameStateId}/characters/{characterId}/conditions/tick` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 84 | `POST api/game-states/{gameStateId}/characters/{characterId}/currency/add` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 85 | `POST api/game-states/{gameStateId}/characters/{characterId}/currency/spend` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 86 | `POST api/game-states/{gameStateId}/characters/{characterId}/experience` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 87 | `POST api/game-states/{gameStateId}/characters/{characterId}/inventory` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 88 | `POST api/game-states/{gameStateId}/characters/{characterId}/inventory/items` | alias-only | Не показывается отдельно; UI использует канонический маршрут |
| 89 | `POST api/game-states/{gameStateId}/characters/{characterId}/inventory/items/{itemId}/equip` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 90 | `POST api/game-states/{gameStateId}/characters/{characterId}/inventory/items/{itemId}/unequip` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 91 | `POST api/game-states/{gameStateId}/characters/{characterId}/inventory/items/{itemId}/use` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 92 | `POST api/game-states/{gameStateId}/characters/{characterId}/knockout` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 93 | `POST api/game-states/{gameStateId}/characters/{characterId}/level-up` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 94 | `POST api/game-states/{gameStateId}/characters/{characterId}/limited-resources` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 95 | `POST api/game-states/{gameStateId}/characters/{characterId}/proficiencies` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 96 | `POST api/game-states/{gameStateId}/characters/{characterId}/revive` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 97 | `POST api/game-states/{gameStateId}/characters/{characterId}/xp` | alias-only | Не показывается отдельно; UI использует канонический маршрут |
| 98 | `POST api/game-states/{gameStateId}/checks/ability` | management/history | `/games/:id/manage/history` |
| 99 | `POST api/game-states/{gameStateId}/combat/apply-damage` | management/encounters | `/games/:id/manage/encounters` |
| 100 | `POST api/game-states/{gameStateId}/combat/attack` | management/encounters | `/games/:id/manage/encounters` |
| 101 | `POST api/game-states/{gameStateId}/combat/end` | management/encounters | `/games/:id/manage/encounters` |
| 102 | `POST api/game-states/{gameStateId}/combat/next-turn` | management/encounters | `/games/:id/manage/encounters` |
| 103 | `POST api/game-states/{gameStateId}/combat/participants` | management/encounters | `/games/:id/manage/encounters` |
| 104 | `POST api/game-states/{gameStateId}/combat/participants/{participantId}/heal` | management/encounters | `/games/:id/manage/encounters` |
| 105 | `POST api/game-states/{gameStateId}/combat/start` | management/encounters | `/games/:id/manage/encounters` |
| 106 | `POST api/game-states/{gameStateId}/loot` | management/encounters | `/games/:id/manage/encounters` |
| 107 | `POST api/game-states/{gameStateId}/loot/{lootContainerId}/claim` | management/encounters | `/games/:id/manage/encounters` |
| 108 | `POST api/game-states/{gameStateId}/mechanic-requests/{requestId}/resolve/ability-check` | management/history | `/games/:id/manage/history` |
| 109 | `POST api/game-states/{gameStateId}/memory/summarize` | management/story | `/games/:id/manage/story` |
| 110 | `POST api/game-states/{gameStateId}/monsters` | alias-only | Не показывается отдельно; UI использует канонический маршрут |
| 111 | `POST api/game-states/{gameStateId}/monsters/spawn` | management/world | `/games/:id/manage/world` |
| 112 | `POST api/game-states/{gameStateId}/monsters/{monsterId}/kill` | management/world | `/games/:id/manage/world` |
| 113 | `POST api/game-states/{gameStateId}/party` | management/story | `/games/:id/manage/story` |
| 114 | `POST api/game-states/{gameStateId}/party/members` | management/story | `/games/:id/manage/story` |
| 115 | `POST api/game-states/{gameStateId}/play/act` | game | `/games/:id/play` |
| 116 | `POST api/game-states/{gameStateId}/play/apply-change/{changeId}` | game | `/games/:id/play` |
| 117 | `POST api/game-states/{gameStateId}/play/apply-safe-changes` | game | `/games/:id/play` |
| 118 | `POST api/game-states/{gameStateId}/play/bootstrap` | game | `/games/:id/play` |
| 119 | `POST api/game-states/{gameStateId}/play/combat/action` | game | `/games/:id/play` |
| 120 | `POST api/game-states/{gameStateId}/play/combat/continue` | game | `/games/:id/play` |
| 121 | `POST api/game-states/{gameStateId}/play/combat/end` | game | `/games/:id/play` |
| 122 | `POST api/game-states/{gameStateId}/play/combat/resolve-outcome` | game | `/games/:id/play` |
| 123 | `POST api/game-states/{gameStateId}/play/combat/start` | game | `/games/:id/play` |
| 124 | `POST api/game-states/{gameStateId}/play/continue` | game | `/games/:id/play` |
| 125 | `POST api/game-states/{gameStateId}/play/location/move` | alias-only | Не показывается отдельно; UI использует канонический маршрут |
| 126 | `POST api/game-states/{gameStateId}/play/message` | alias-only | Не показывается отдельно; UI использует канонический маршрут |
| 127 | `POST api/game-states/{gameStateId}/play/reject-change/{changeId}` | game | `/games/:id/play` |
| 128 | `POST api/game-states/{gameStateId}/play/resolve-and-continue/{requestId}` | game | `/games/:id/play` |
| 129 | `POST api/game-states/{gameStateId}/play/resolve-mechanic-request/{requestId}` | alias-only | Не показывается отдельно; UI использует канонический маршрут |
| 130 | `POST api/game-states/{gameStateId}/play/start` | game | `/games/:id/play` |
| 131 | `POST api/game-states/{gameStateId}/play/summarize` | alias-only | Не показывается отдельно; UI использует канонический маршрут |
| 132 | `POST api/game-states/{gameStateId}/play/travel` | game | `/games/:id/play` |
| 133 | `POST api/game-states/{gameStateId}/quests/{questId}/complete` | management/encounters | `/games/:id/manage/encounters` |
| 134 | `POST api/game-states/{gameStateId}/quests/{questId}/rewards/grant` | management/encounters | `/games/:id/manage/encounters` |
| 135 | `POST api/game-states/{gameStateId}/rest/long` | game | `/games/:id/play` |
| 136 | `POST api/game-states/{gameStateId}/rest/short` | game | `/games/:id/play` |
| 137 | `POST api/game-states/{gameStateId}/rolls` | management/history | `/games/:id/manage/history` |
| 138 | `POST api/game-states/{gameStateId}/time/advance` | game | `/games/:id/play` |
| 139 | `POST api/game-states/{gameStateId}/turns` | management/history | `/games/:id/manage/history` |
| 140 | `POST api/game-states/{gameStateId}/world/containers` | management/world | `/games/:id/manage/world` |
| 141 | `POST api/game-states/{gameStateId}/world/factions` | management/world | `/games/:id/manage/world` |
| 142 | `POST api/game-states/{gameStateId}/world/locations` | management/world | `/games/:id/manage/world` |
| 143 | `POST api/game-states/{gameStateId}/world/locations/{locationId}/exits` | management/world | `/games/:id/manage/world` |
| 144 | `POST api/game-states/{gameStateId}/world/monsters` | management/world | `/games/:id/manage/world` |
| 145 | `POST api/game-states/{gameStateId}/world/npcs` | management/world | `/games/:id/manage/world` |
| 146 | `POST api/game-states/{gameStateId}/world/objects` | management/world | `/games/:id/manage/world` |
| 147 | `POST api/game-states/{gameStateId}/world/quests` | management/world | `/games/:id/manage/world` |
| 148 | `POST api/game-states/{gameStateId}/world/quests/{questId}/steps` | management/world | `/games/:id/manage/world` |
| 149 | `PUT api/game-states/{gameStateId}/characters/{characterId}` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 150 | `PUT api/game-states/{gameStateId}/characters/{characterId}/abilities/{abilityId}` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 151 | `PUT api/game-states/{gameStateId}/characters/{characterId}/attacks/{attackId}` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 152 | `PUT api/game-states/{gameStateId}/characters/{characterId}/attributes` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 153 | `PUT api/game-states/{gameStateId}/characters/{characterId}/combat-stats` | management/encounters | `/games/:id/manage/encounters` |
| 154 | `PUT api/game-states/{gameStateId}/characters/{characterId}/conditions/{conditionId}` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 155 | `PUT api/game-states/{gameStateId}/characters/{characterId}/equipment` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 156 | `PUT api/game-states/{gameStateId}/characters/{characterId}/inventory/{itemId}` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 157 | `PUT api/game-states/{gameStateId}/characters/{characterId}/limited-resources/{resourceId}` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 158 | `PUT api/game-states/{gameStateId}/characters/{characterId}/needs` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 159 | `PUT api/game-states/{gameStateId}/characters/{characterId}/progression` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 160 | `PUT api/game-states/{gameStateId}/characters/{characterId}/resources` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 161 | `PUT api/game-states/{gameStateId}/characters/{characterId}/wealth` | management/characters | `/games/:id/manage/characters`; player-safe subset also in `/play` |
| 162 | `PUT api/game-states/{gameStateId}/memory` | management/story | `/games/:id/manage/story` |
| 163 | `PUT api/game-states/{gameStateId}/story` | management/story | `/games/:id/manage/story` |
| 164 | `PUT api/game-states/{gameStateId}/world/containers/{containerId}` | management/world | `/games/:id/manage/world` |
| 165 | `PUT api/game-states/{gameStateId}/world/factions/{factionId}` | management/world | `/games/:id/manage/world` |
| 166 | `PUT api/game-states/{gameStateId}/world/locations/{locationId}` | management/world | `/games/:id/manage/world` |
| 167 | `PUT api/game-states/{gameStateId}/world/locations/{locationId}/exits/{exitId}` | management/world | `/games/:id/manage/world` |
| 168 | `PUT api/game-states/{gameStateId}/world/monsters/{monsterId}` | management/world | `/games/:id/manage/world` |
| 169 | `PUT api/game-states/{gameStateId}/world/npcs/{npcId}` | management/world | `/games/:id/manage/world` |
| 170 | `PUT api/game-states/{gameStateId}/world/objects/{objectId}` | management/world | `/games/:id/manage/world` |
| 171 | `PUT api/game-states/{gameStateId}/world/quests/{questId}` | management/world | `/games/:id/manage/world` |
| 172 | `PUT api/game-states/{gameStateId}/world/quests/{questId}/steps/{stepId}` | management/world | `/games/:id/manage/world` |

## Alias policy

Alias-only routes are kept for API compatibility. Their canonical equivalents are used by the UI: inventory `/inventory`, progression `/experience`, world `/world/monsters`, mechanic requests `/mechanic-requests/.../resolve/ability-check`, memory `/memory/summarize`, turns `/turns`, and travel `/play/travel`.
