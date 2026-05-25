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
- `game.inventory_items` - inventory list with item type, damage, weight, quantity, armor class, and armor class bonus.
- `game.equipped_gear` - equipped item slots.
- `game.combat_stats` - armor class and proficiency bonus.
- `game.attacks` - character attacks.

`game.player_documents` is a view that returns the whole player as JSON shaped like the current C# `Player` model JSON names.

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

INSERT INTO game.inventory_items (player_id, item_order, name, item_type, damage, weight, quantity, armor_class_bonus)
VALUES
    ('<player-id>', 0, 'Longsword', 'weapon', '1d8 slashing', 1.5, 1, NULL),
    ('<player-id>', 1, 'Shield', 'shield', NULL, 3, 1, 2);

INSERT INTO game.attacks (player_id, attack_order, name, roll, damage)
VALUES
    ('<player-id>', 0, 'Longsword', '+5', '1d8+3 slashing');

SELECT data
FROM game.player_documents
WHERE player_id = '<player-id>';
```
