import { Attributes } from "../types/player";

export default function AttributesBlock({
  attributes,
}: {
  attributes: Attributes;
}) {
  return (
    <div className="card">
      <h2>Характеристики</h2>

      <ul>
        <li>Сила: {attributes.strength}</li>
        <li>Ловкость: {attributes.dexterity}</li>
        <li>Телосложение: {attributes.constitution}</li>
        <li>Интеллект: {attributes.intelligence}</li>
        <li>Мудрость: {attributes.wisdom}</li>
        <li>Харизма: {attributes.charisma}</li>
      </ul>
    </div>
  );
}