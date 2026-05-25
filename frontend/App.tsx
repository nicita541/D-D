import { useEffect, useState } from "react";
import { Player } from "./types/player";

import CharacterCard from "./components/CharacterCard";
import AttributesBlock from "./components/AttributesBlock";
import InventoryBlock from "./components/InventoryBlock";
import ResourcesBlock from "./components/ResourcesBlock";

function App() {
  const [player, setPlayer] = useState<Player | null>(null);

  useEffect(() => {
    fetch("https://localhost:7057/api/player")
      .then(res => res.json())
      .then(data => setPlayer(data))
      .catch(err => console.error(err));
  }, []);

  if (!player) {
    return <div>Загрузка персонажа...</div>;
  }

  return (
    <div className="container">
      <CharacterCard character={player.character} />

      <ResourcesBlock resources={player.resources} />

      <AttributesBlock attributes={player.attributes} />

      <InventoryBlock inventory={player.inventory} />
    </div>
  );
}

export default App;