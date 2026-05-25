import { Character } from "../types/player";

export default function CharacterCard({
  character,
}: {
  character: Character;
}) {
  return (
    <div className="card">
      <h1>{character.name}</h1>

      <p>{character.species}</p>

      <p>
        {character.class} / {character.subclass}
      </p>

      <p>Уровень: {character.level}</p>

      <p>{character.background}</p>
    </div>
  );
}