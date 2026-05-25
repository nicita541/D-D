# D&D PostgreSQL database

Dockerized PostgreSQL database for storing `Player` characters from the C# model.

## Start

```powershell
docker compose up -d --build
```

Connection settings:

- Host: `localhost`
- Port: `5432`
- Database: `dnd`
- User: `dnd_user`
- Password: `dnd_password`

Connection string for .NET/Npgsql:

```text
Host=localhost;Port=5432;Database=dnd;Username=dnd_user;Password=dnd_password
```

## Reset database

Postgres runs init scripts only when the data volume is empty.

```powershell
docker compose down -v
docker compose up -d --build
```

## Main tables

- `game.players` - character identity: name, background, species, class, subclass, level, experience.
- `game.resources` - hp, mana, action points.
- `game.conditions` - active conditions.
- `game.attributes` - strength, intelligence, dexterity, wisdom, constitution, charisma, initiative, speed, perception.
- `game.equipment_proficiencies` and `game.weapon_other_proficiencies` - armor and weapon proficiencies.
- `game.abilities` - class abilities, species abilities, feats.
- `game.needs` - food, water, carrying capacity.
- `game.movement` - km per turn.
- `game.wealth` - coins.
- `game.inventory_items` - inventory list.
- `game.equipped_gear` - equipped item slots.

`game.player_documents` is a view that returns the whole player as JSON shaped like the C# `Player` model.

## Example

```sql
INSERT INTO game.players (name, background, species, class_name, subclass, level, experience)
VALUES ('Arvel', 'Soldier', 'Human', 'Fighter', 'Champion', 1, 0)
RETURNING id;
```

Default rows for resources, attributes, proficiencies, needs, movement, wealth, and equipped gear are created automatically.

```sql
UPDATE game.resources
SET hp_max = 12, hp_current = 12, action_points_max = 1, action_points_current = 1
WHERE player_id = '<player-id>';

INSERT INTO game.inventory_items (player_id, item_order, name)
VALUES
    ('<player-id>', 0, 'Longsword'),
    ('<player-id>', 1, 'Shield');

SELECT data
FROM game.player_documents
WHERE player_id = '<player-id>';
```
