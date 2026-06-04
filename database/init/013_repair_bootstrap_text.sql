-- Repairs only exact values created by the previously corrupted play bootstrap.
-- Arbitrary user and AI text is intentionally left untouched.

UPDATE game.locations
SET name = 'Старая дорога',
    description = 'Пыльная дорога у кромки леса. Рядом стоит покосившийся указатель и видны свежие следы.'
WHERE encode(convert_to(name, 'UTF8'), 'hex') = 'efbfbdefbfbdefbfbdefbfbdefbfbdefbfbd20efbfbdefbfbdefbfbdefbfbdefbfbdefbfbd'
  AND encode(convert_to(description, 'UTF8'), 'hex') = 'efbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbd20efbfbdefbfbdefbfbdefbfbdefbfbdefbfbd20efbfbd20efbfbdefbfbdefbfbdefbfbdefbfbdefbfbd20efbfbdefbfbdefbfbdefbfbd2e20efbfbdefbfbdefbfbdefbfbdefbfbd20efbfbdefbfbdefbfbdefbfbdefbfbd20efbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbd20efbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbd20efbfbd20efbfbdefbfbdefbfbdefbfbdefbfbd20efbfbdefbfbdefbfbdefbfbdefbfbdefbfbd20efbfbdefbfbdefbfbdefbfbdefbfbd2e';

UPDATE game.quests
SET description = 'Разобраться, что случилось у старой дороги, и найти источник тревожных следов.'
WHERE title = 'Первый след'
  AND encode(convert_to(description, 'UTF8'), 'hex') = 'efbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbd2c20efbfbdefbfbdefbfbd20efbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbd20efbfbd20efbfbdefbfbdefbfbdefbfbdefbfbdefbfbd20efbfbdefbfbdefbfbdefbfbdefbfbdefbfbd2c20efbfbd20efbfbdefbfbdefbfbdefbfbdefbfbd20efbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbd20efbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbdefbfbd20efbfbdefbfbdefbfbdefbfbdefbfbdefbfbd2e';
