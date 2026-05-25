export default function InventoryBlock({
  inventory,
}: {
  inventory: string[];
}) {
  return (
    <div className="card">
      <h2>Инвентарь</h2>

      <ul>
        {inventory.map((item, index) => (
          <li key={index}>{item}</li>
        ))}
      </ul>
    </div>
  );
}