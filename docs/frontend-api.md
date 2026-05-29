# Frontend API contract

Документ описывает текущий backend contract для frontend MVP. Все игровые endpoints, кроме публичных campaign reads и health, требуют JWT access token:

```http
Authorization: Bearer <accessToken>
```

Canonical стиль для RPG gameplay request body — русские JSON-поля. Если backend уже принимает старые английские aliases, они сохранены для совместимости, но frontend должен отправлять русскую форму из этого документа.

## Auth

`POST /api/auth/register`

```json
{
  "email": "hero@example.com",
  "username": "hero",
  "password": "strong-password",
  "displayName": "Герой"
}
```

`POST /api/auth/login`

```json
{
  "emailOrUsername": "hero@example.com",
  "password": "strong-password"
}
```

`POST /api/auth/refresh`

```json
{
  "refreshToken": "<refreshToken>"
}
```

`POST /api/auth/logout`

```json
{
  "refreshToken": "<refreshToken>"
}
```

`GET /api/auth/me`

Auth response:

```json
{
  "accessToken": "<jwt>",
  "refreshToken": "<long-refresh-token>",
  "accessTokenExpiresAt": "2026-05-29T12:00:00Z",
  "account": {
    "id": "<accountId>",
    "email": "hero@example.com",
    "username": "hero",
    "displayName": "Герой",
    "role": "user"
  }
}
```

## GameStates

`POST /api/game-states`

```json
{
  "название": "Тестовая игра"
}
```

`GET /api/game-states`

`GET /api/game-states/{gameStateId}`

## Characters

`POST /api/game-states/{gameStateId}/characters`

```json
{
  "имя": "Торвен",
  "предыстория": "Бывший страж каравана.",
  "вид": "человек",
  "класс": "воин",
  "подкласс": null,
  "описание": "Широкоплечий воин с потёртым плащом.",
  "мировоззрение": "нейтральный добрый",
  "прогресс": {
    "уровень": 1,
    "опыт": 0,
    "опытДоСледующегоУровня": 300
  },
  "характеристики": {
    "сила": 16,
    "ловкость": 12,
    "телосложение": 14,
    "интеллект": 10,
    "мудрость": 11,
    "харизма": 9,
    "инициатива": 1,
    "скорость": 9,
    "восприятие": 11
  },
  "ресурсы": {
    "хпМаксимум": 12,
    "хпТекущее": 12,
    "манаМаксимум": 0,
    "манаТекущая": 0,
    "очкиДействийМаксимум": 1,
    "очкиДействийТекущие": 1
  },
  "богатство": {
    "медные": 0,
    "серебряные": 0,
    "золотые": 10,
    "платиновые": 0
  },
  "бой": {
    "классДоспеха": 14,
    "бонусМастерства": 2,
    "вБою": false,
    "бросокИнициативы": 0
  }
}
```

`GET /api/game-states/{gameStateId}/characters`

`GET /api/game-states/{gameStateId}/characters/{characterId}`

`PUT /api/game-states/{gameStateId}/characters/{characterId}` принимает те же поля, все поля optional.

`DELETE /api/game-states/{gameStateId}/characters/{characterId}`

## Inventory

`POST /api/game-states/{gameStateId}/characters/{characterId}/inventory`

```json
{
  "название": "Старый меч",
  "тип": "weapon",
  "подтип": "sword",
  "описание": "Потёртый, но надёжный меч.",
  "количество": 1,
  "стакуемый": false,
  "вес": 1.5,
  "состояние": "worn",
  "редкость": "common",
  "магический": false,
  "золотые": 5,
  "теги": ["оружие", "меч"]
}
```

`GET /api/game-states/{gameStateId}/characters/{characterId}/inventory`

`PUT /api/game-states/{gameStateId}/characters/{characterId}/inventory/{itemId}`

`DELETE /api/game-states/{gameStateId}/characters/{characterId}/inventory/{itemId}`

## Equipment

`PUT /api/game-states/{gameStateId}/characters/{characterId}/equipment`

```json
{
  "голова": null,
  "тело": null,
  "руки": null,
  "ноги": null,
  "обувь": null,
  "основнаяРука": "<itemId>",
  "втораяРука": null,
  "амулет": null,
  "кольцо1": null,
  "кольцо2": null
}
```

`GET /api/game-states/{gameStateId}/characters/{characterId}/equipment`

Перед equip item должен принадлежать inventory этого character.

## Attacks

`POST /api/game-states/{gameStateId}/characters/{characterId}/attacks`

```json
{
  "предметId": "<itemId>",
  "название": "Удар мечом",
  "бросок": "1d20+4",
  "урон": "1d8+2",
  "типУрона": "рубящий"
}
```

`GET /api/game-states/{gameStateId}/characters/{characterId}/attacks`

`PUT /api/game-states/{gameStateId}/characters/{characterId}/attacks/{attackId}`

`DELETE /api/game-states/{gameStateId}/characters/{characterId}/attacks/{attackId}`

## Rolls

`POST /api/game-states/{gameStateId}/rolls`

Canonical body:

```json
{
  "формула": "1d20+4",
  "причина": "Проверка силы",
  "персонажId": "<characterId or null>"
}
```

Compatibility body:

```json
{
  "formula": "1d20+4",
  "reason": "Проверка силы",
  "characterId": "<characterId or null>"
}
```

Response:

```json
{
  "id": "<rollId>",
  "gameStateId": "<gameStateId>",
  "characterId": "<characterId>",
  "formula": "1d20+4",
  "reason": "Проверка силы",
  "rolls": [12],
  "modifier": 4,
  "total": 16,
  "createdAt": "2026-05-29T12:00:00Z"
}
```

`GET /api/game-states/{gameStateId}/rolls?limit=50`

Поддержанные формулы: `d20`, `1d20`, `1d20+4`, `1d20-1`, `2d6`, `1d8+2`.

## Ability Checks

`POST /api/game-states/{gameStateId}/checks/ability`

Canonical body:

```json
{
  "персонажId": "<characterId>",
  "характеристика": "ловкость",
  "сложность": 14,
  "причина": "Перепрыгнуть через провал"
}
```

Compatibility body:

```json
{
  "characterId": "<characterId>",
  "ability": "dexterity",
  "difficultyClass": 14,
  "reason": "Перепрыгнуть через провал"
}
```

Response:

```json
{
  "id": "<checkId>",
  "roll": {
    "id": "<rollId>",
    "formula": "1d20+1",
    "rolls": [14],
    "modifier": 1,
    "total": 15
  },
  "characterId": "<characterId>",
  "ability": "ловкость",
  "abilityScore": 12,
  "modifier": 1,
  "difficultyClass": 14,
  "total": 15,
  "success": true,
  "reason": "Перепрыгнуть через провал"
}
```

`GET /api/game-states/{gameStateId}/checks?limit=50`

Характеристики: `сила`, `ловкость`, `телосложение`, `интеллект`, `мудрость`, `харизма`. Английские aliases тоже принимаются.

## World / Monsters

`POST /api/game-states/{gameStateId}/world/monsters`

```json
{
  "название": "Пещерный волк",
  "тип": "beast",
  "описание": "Голодный серый волк из сырой пещеры.",
  "локацияId": null,
  "хпТекущее": 9,
  "хпМаксимум": 9,
  "классДоспеха": 12,
  "инициатива": 2,
  "статы": {
    "сила": 12,
    "ловкость": 14
  },
  "способности": [],
  "добыча": [],
  "теги": ["зверь", "волк"]
}
```

`GET /api/game-states/{gameStateId}/world/monsters`

`GET /api/game-states/{gameStateId}/world/monsters/{monsterId}`

`PUT /api/game-states/{gameStateId}/world/monsters/{monsterId}`

`DELETE /api/game-states/{gameStateId}/world/monsters/{monsterId}`

Другие world groups: `locations`, `objects`, `containers`, `npcs`, `factions`, `quests`, `quests/{questId}/steps`.

## Combat

`POST /api/game-states/{gameStateId}/combat/start`

```json
{
  "участники": []
}
```

`POST /api/game-states/{gameStateId}/combat/participants`

Character participant:

```json
{
  "типАктера": "character",
  "actorId": "<characterId>",
  "имя": "Торвен",
  "инициатива": 13,
  "хпТекущее": 12,
  "хпМаксимум": 12,
  "классДоспеха": 14,
  "состояния": []
}
```

Monster participant:

```json
{
  "типАктера": "monster",
  "actorId": "<monsterId>",
  "имя": "Пещерный волк",
  "инициатива": 11,
  "хпТекущее": 9,
  "хпМаксимум": 9,
  "классДоспеха": 12,
  "состояния": []
}
```

`GET /api/game-states/{gameStateId}/combat`

`POST /api/game-states/{gameStateId}/combat/next-turn`

Переключает активного участника по инициативе. Порядок: `инициатива desc`, затем `created_at asc`, затем `id asc`. При переходе с последнего участника на первого увеличивает `раунд`.

`POST /api/game-states/{gameStateId}/combat/apply-damage`

```json
{
  "цельУчастникId": "<participantId>",
  "урон": 5,
  "типУрона": "рубящий",
  "причина": "Удар мечом"
}
```

HP не уходит ниже 0. Если HP становится 0, backend добавляет состояние `"повержен"` без дублей.

`POST /api/game-states/{gameStateId}/combat/participants/{participantId}/heal`

```json
{
  "лечение": 4,
  "причина": "Зелье лечения"
}
```

Лечение не поднимает HP выше `хпМаксимум`. Состояние `"повержен"` автоматически не снимается.

`POST /api/game-states/{gameStateId}/combat/attack`

```json
{
  "атакующийУчастникId": "<participantId>",
  "цельУчастникId": "<participantId>",
  "бросокАтаки": "1d20+4",
  "урон": "1d8+2",
  "типУрона": "рубящий",
  "причина": "Удар мечом"
}
```

Backend бросает атаку, сравнивает total с `классДоспеха` цели, при попадании бросает урон и применяет его. Эти combat rolls пока возвращаются в response как ephemeral result и не пишутся в `game.dice_rolls`.

`POST /api/game-states/{gameStateId}/combat/end`

## XP / Level Up

`POST /api/game-states/{gameStateId}/characters/{characterId}/experience`

```json
{
  "опыт": 50,
  "причина": "Победа над волком"
}
```

Response:

```json
{
  "characterId": "<characterId>",
  "experience": 350,
  "addedExperience": 50,
  "reason": "Победа над волком",
  "level": 1,
  "experienceToNextLevel": 300,
  "canLevelUp": true
}
```

`POST /api/game-states/{gameStateId}/characters/{characterId}/level-up`

```json
{
  "новыйУровень": 2,
  "хпМаксимумДобавить": 7
}
```

`новыйУровень` сейчас должен быть строго `current level + 1`. Backend обновляет `level`, `experienceToNextLevel`, `hp_max` и `hp_current`.

## Turns

`POST /api/game-states/{gameStateId}/turns`

Canonical body:

```json
{
  "сообщение": "Я осматриваюсь вокруг и пытаюсь понять, где нахожусь."
}
```

Compatibility body accepted by backend:

```json
{
  "message": "Я осматриваюсь вокруг и пытаюсь понять, где нахожусь."
}
```

Response fields include:

```json
{
  "id": "<turnId>",
  "gameStateId": "<gameStateId>",
  "turnNumber": 1,
  "status": "completed",
  "playerMessage": "...",
  "masterAnswer": "...",
  "rawAiResponse": {},
  "aiModel": "qwen2.5:7b",
  "errorMessage": null,
  "changes": []
}
```

Statuses:

- `pending`: ход создан и обрабатывается.
- `completed`: AI ответ прошёл validation, изменения сохранены как proposals.
- `failed`: AI/Ollama/validation ошибка; игровые таблицы не изменены.

Если в игре уже есть `pending` turn, новый `POST /turns` возвращает `409`.

## Campaign Memory

`GET /api/game-states/{gameStateId}/memory`

`PUT /api/game-states/{gameStateId}/memory`

Canonical body:

```json
{
  "резюме": "Герои прибыли к старому колодцу.",
  "текущаяСцена": {
    "место": "Старый колодец",
    "настроение": "тревожное"
  },
  "важныеФакты": ["У колодца найден знак культа."],
  "открытыеЛинии": ["Выяснить, кто оставил знак."],
  "закрытыеЛинии": [],
  "известныеNpc": [],
  "известныеЛокации": [{ "название": "Старый колодец" }],
  "секретыМастера": []
}
```

Compatibility aliases: `summary`, `currentScene`, `importantFacts`, `openThreads`, `resolvedThreads`, `knownNpcs`, `knownLocations`, `masterSecrets`.

Важно: `секретыМастера` backend возвращает для будущего GM mode, но обычный player UI не должен показывать это поле игроку.

`POST /api/game-states/{gameStateId}/memory/summarize`

```json
{
  "последниеЗаписи": 20
}
```

Backend берёт последние записи `game_log_entries`, вызывает Ollama строгим JSON-only prompt, валидирует patch памяти и применяет его тем же merge-алгоритмом, что `обновить_память`. Если журнал пустой, вернёт `400`. Если AI вернул невалидный JSON, вернёт `503` без частичного изменения памяти.

## Mechanic Requests

`GET /api/game-states/{gameStateId}/mechanic-requests?status=pending`

Response item:

```json
{
  "id": "<requestId>",
  "gameStateId": "<gameStateId>",
  "turnId": "<turnId>",
  "changeId": "<changeId>",
  "requestType": "ability_check",
  "payload": {
    "тип": "ability_check",
    "персонажId": null,
    "характеристика": "ловкость",
    "сложность": 14,
    "причина": "Перепрыгнуть через провал"
  },
  "status": "pending",
  "result": {},
  "createdAt": "2026-05-29T12:00:00Z",
  "resolvedAt": null
}
```

`POST /api/game-states/{gameStateId}/mechanic-requests/{requestId}/resolve/ability-check`

```json
{
  "персонажId": "<characterId>"
}
```

Если `персонажId` не передан в body, backend попытается взять его из payload request. Если его всё равно нет, вернёт `400`.

## Pending Changes

`GET /api/game-states/{gameStateId}/changes?status=pending`

`POST /api/game-states/{gameStateId}/changes/{changeId}/apply`

`POST /api/game-states/{gameStateId}/changes/{changeId}/reject`

Reject body:

```json
{
  "причина": "Не подходит текущей сцене."
}
```

Поддержанные operations в AI response: `добавить_предмет`, `изменить_хп`, `изменить_ресурс`, `добавить_состояние`, `удалить_состояние`, `обновить_квест`, `добавить_запись_журнала`, `переместить_предмет`, `запросить_бросок`, `обновить_память`.

Новые mechanics/memory operations:

```json
{
  "operation": "запросить_бросок",
  "payload": {
    "тип": "ability_check",
    "персонажId": "<characterId or null>",
    "характеристика": "ловкость",
    "сложность": 14,
    "причина": "Перепрыгнуть через провал"
  }
}
```

```json
{
  "operation": "обновить_память",
  "payload": {
    "добавитьКРезюме": "Герои нашли знак культа у старого колодца.",
    "текущаяСцена": {},
    "важныеФактыДобавить": [],
    "открытыеЛинииДобавить": [],
    "закрытыеЛинииДобавить": [],
    "известныеNpcОбновить": [],
    "известныеЛокацииОбновить": [],
    "секретыМастераДобавить": []
  }
}
```

`apply` для `запросить_бросок` создаёт `game.mechanic_requests` со status `pending`. `apply` для `обновить_память` обновляет `campaign_memories`: summary append, shallow merge `текущаяСцена`, append/dedup массивов по `name` / `название` / `title` или raw JSON.

## Full MVP Scenario

1. `POST /api/auth/register` или `POST /api/auth/login`.
2. Сохранить `accessToken` и отправлять `Authorization: Bearer <accessToken>`.
3. `POST /api/game-states` создать игру.
4. `POST /api/game-states/{gameStateId}/characters` создать персонажа.
5. `POST /api/game-states/{gameStateId}/characters/{characterId}/inventory` добавить предмет.
6. `PUT /api/game-states/{gameStateId}/characters/{characterId}/equipment` экипировать предмет.
7. `POST /api/game-states/{gameStateId}/characters/{characterId}/attacks` добавить атаку.
8. `POST /api/game-states/{gameStateId}/world/monsters` создать монстра.
9. `POST /api/game-states/{gameStateId}/combat/start` начать бой.
10. `POST /api/game-states/{gameStateId}/combat/participants` добавить character participant.
11. `POST /api/game-states/{gameStateId}/combat/participants` добавить monster participant.
12. `POST /api/game-states/{gameStateId}/combat/next-turn` выставить активного участника.
13. `POST /api/game-states/{gameStateId}/combat/attack` провести простую атаку.
14. `POST /api/game-states/{gameStateId}/characters/{characterId}/experience` начислить XP при необходимости.
15. `POST /api/game-states/{gameStateId}/turns` отправить сообщение игрока.
16. `GET /api/game-states/{gameStateId}/changes?status=pending` увидеть предложенные изменения.
17. `POST /api/game-states/{gameStateId}/changes/{changeId}/apply` применить изменение или `/reject` отклонить.

## Mechanics / Memory Flow

Действие, которому нужен бросок:

1. Frontend отправляет `POST /turns`.
2. AI может вернуть pending change `operation = "запросить_бросок"`.
3. Frontend применяет change через `POST /changes/{changeId}/apply`.
4. Backend создаёт `mechanic_request`.
5. Frontend показывает игроку требуемый бросок.
6. Frontend вызывает `POST /mechanic-requests/{requestId}/resolve/ability-check`.
7. Результат проверки можно отправить следующим `POST /turns` в тексте сообщения игрока.

Долгая память кампании:

1. Frontend может читать память через `GET /memory`.
2. Frontend или GM-инструмент может обновлять память через `PUT /memory`.
3. AI context следующего хода включает `памятьКампании`.
4. AI может предложить `operation = "обновить_память"`.
5. Frontend применяет change через `POST /changes/{changeId}/apply`.
6. Следующий AI context содержит обновлённую память.

## Health

`GET /health`

```json
{
  "status": "ok"
}
```

`GET /health/db`

```json
{
  "status": "ok",
  "database": "ok"
}
```
