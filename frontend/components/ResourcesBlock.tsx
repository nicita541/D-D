import { Resources } from "../types/player";

export default function ResourcesBlock({
  resources,
}: {
  resources: Resources;
}) {
  return (
    <div className="card">
      <h2>Ресурсы</h2>

      <p>
        HP: {resources.hp.current}/{resources.hp.max}
      </p>

      <p>
        Mana: {resources.mana.current}/{resources.mana.max}
      </p>

      <p>
        AP: {resources.actionPoints.current}/
        {resources.actionPoints.max}
      </p>
    </div>
  );
}