using System.Text.Json.Serialization;

namespace backend.Models;

public class WorldState
{
    [JsonPropertyName("текущаялокацияId")]
    public string CurrentLocationId { get; set; } = "";

    [JsonPropertyName("локации")]
    public Dictionary<string, Location> Locations { get; set; } = new();

    [JsonPropertyName("нпс")]
    public Dictionary<string, Npc> Npcs { get; set; } = new();

    [JsonPropertyName("фракции")]
    public Dictionary<string, Faction> Factions { get; set; } = new();
}

public class Location
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = $"location_{Guid.NewGuid():N}";

    [JsonPropertyName("название")]
    public string Name { get; set; } = "";

    [JsonPropertyName("описание")]
    public string Description { get; set; } = "";

    [JsonPropertyName("предметы")]
    public List<InventoryItem> Items { get; set; } = new();

    [JsonPropertyName("контейнеры")]
    public List<WorldContainer> Containers { get; set; } = new();

    [JsonPropertyName("выходы")]
    public List<LocationExit> Exits { get; set; } = new();

    [JsonPropertyName("объекты")]
    public List<WorldObject> Objects { get; set; } = new();

    [JsonPropertyName("нпсIds")]
    public List<string> NpcIds { get; set; } = new();
}

public class WorldContainer
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = $"container_{Guid.NewGuid():N}";

    [JsonPropertyName("название")]
    public string Name { get; set; } = "";

    [JsonPropertyName("описание")]
    public string Description { get; set; } = "";

    [JsonPropertyName("закрыт")]
    public bool IsLocked { get; set; }

    [JsonPropertyName("ключId")]
    public string? KeyItemId { get; set; }

    [JsonPropertyName("предметы")]
    public List<InventoryItem> Items { get; set; } = new();
}

public class LocationExit
{
    [JsonPropertyName("направление")]
    public string Direction { get; set; } = "";

    [JsonPropertyName("локацияId")]
    public string LocationId { get; set; } = "";

    [JsonPropertyName("описание")]
    public string? Description { get; set; }

    [JsonPropertyName("закрыт")]
    public bool IsLocked { get; set; }
}

public class WorldObject
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = $"object_{Guid.NewGuid():N}";

    [JsonPropertyName("название")]
    public string Name { get; set; } = "";

    [JsonPropertyName("тип")]
    public string Type { get; set; } = ""; // колодец, дверь, алтарь, ловушка, костер, труп

    [JsonPropertyName("описание")]
    public string Description { get; set; } = "";

    [JsonPropertyName("состояние")]
    public string State { get; set; } = "обычное";

    [JsonPropertyName("предметы")]
    public List<InventoryItem> Items { get; set; } = new();

    [JsonPropertyName("теги")]
    public List<string> Tags { get; set; } = new();
}

public class Npc
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = $"npc_{Guid.NewGuid():N}";

    [JsonPropertyName("имя")]
    public string Name { get; set; } = "";

    [JsonPropertyName("роль")]
    public string Role { get; set; } = ""; // торговец, враг, союзник, квестодатель

    [JsonPropertyName("отношение")]
    public string Attitude { get; set; } = "нейтральное";

    [JsonPropertyName("описание")]
    public string Description { get; set; } = "";

    [JsonPropertyName("жив")]
    public bool IsAlive { get; set; } = true;

    [JsonPropertyName("инвентарь")]
    public List<InventoryItem> Inventory { get; set; } = new();
}

public class Faction
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = $"faction_{Guid.NewGuid():N}";

    [JsonPropertyName("название")]
    public string Name { get; set; } = "";

    [JsonPropertyName("отношение")]
    public int Reputation { get; set; }

    [JsonPropertyName("описание")]
    public string Description { get; set; } = "";
}
