using System.Text.Json.Serialization;

namespace backend.Models
{
    public class Player
    {
        [JsonPropertyName("персонаж")]
        public Character Character { get; set; } = new();

        [JsonPropertyName("ресурсы")]
        public Resources Resources { get; set; } = new();

        [JsonPropertyName("характеристики")]
        public Attributes Attributes { get; set; } = new();

        [JsonPropertyName("владениеснаряжением")]
        public EquipmentProficiency EquipmentProficiency { get; set; } = new();

        [JsonPropertyName("способности")]
        public Abilities Abilities { get; set; } = new();

        [JsonPropertyName("потребности")]
        public Needs Needs { get; set; } = new();

        [JsonPropertyName("передвижение")]
        public Movement Movement { get; set; } = new();

        [JsonPropertyName("богатство")]
        public Wealth Wealth { get; set; } = new();

        [JsonPropertyName("инвентарь")]
        public List<InventoryItem> Inventory { get; set; } = new();

        [JsonPropertyName("снаряжениенадетое")]
        public EquippedGear EquippedGear { get; set; } = new();

        [JsonPropertyName("боевыепараметры")]
        public CombatStats CombatStats { get; set; } = new();
    }

    public class Character
    {
        [JsonPropertyName("имя")]
        public string Name { get; set; } = "";

        [JsonPropertyName("предыстория")]
        public string Background { get; set; } = "";

        [JsonPropertyName("вид")]
        public string Species { get; set; } = "";

        [JsonPropertyName("класс")]
        public string Class { get; set; } = "";

        [JsonPropertyName("подкласс")]
        public string Subclass { get; set; } = "";

        [JsonPropertyName("уровень")]
        public int Level { get; set; }

        [JsonPropertyName("опыт")]
        public int Experience { get; set; }
    }

    public class Resources
    {
        [JsonPropertyName("хп")]
        public Stat Hp { get; set; } = new();

        [JsonPropertyName("мана")]
        public Stat Mana { get; set; } = new();

        [JsonPropertyName("очкидействий")]
        public Stat ActionPoints { get; set; } = new();

        [JsonPropertyName("состояния")]
        public Conditions Conditions { get; set; } = new();
    }

    public class Stat
    {
        [JsonPropertyName("максимум")]
        public int Max { get; set; }

        [JsonPropertyName("текущее")]
        public int Current { get; set; }
    }

    public class Conditions
    {
        [JsonPropertyName("ошеломление")]
        public bool Stunned { get; set; }

        [JsonPropertyName("другие")]
        public List<Condition> Other { get; set; } = new();
    }

    public class Condition
    {
        [JsonPropertyName("название")]
        public string Name { get; set; } = "";

        [JsonPropertyName("описание")]
        public string Description { get; set; } = "";

        [JsonPropertyName("длительностьходов")]
        public int? DurationTurns { get; set; }

        [JsonPropertyName("сила")]
        public int? Power { get; set; }

        [JsonPropertyName("постоянное")]
        public bool IsPermanent { get; set; }
    }

    public class Attributes
    {
        [JsonPropertyName("сила")]
        public int Strength { get; set; }

        [JsonPropertyName("интеллект")]
        public int Intelligence { get; set; }

        [JsonPropertyName("ловкость")]
        public int Dexterity { get; set; }

        [JsonPropertyName("мудрость")]
        public int Wisdom { get; set; }

        [JsonPropertyName("телосложение")]
        public int Constitution { get; set; }

        [JsonPropertyName("харизма")]
        public int Charisma { get; set; }

        [JsonPropertyName("инициатива")]
        public int Initiative { get; set; }

        [JsonPropertyName("скорость")]
        public int Speed { get; set; }

        [JsonPropertyName("восприятие")]
        public int Perception { get; set; }
    }

    public class EquipmentProficiency
    {
        [JsonPropertyName("доспехи")]
        public ArmorProficiency Armor { get; set; } = new();

        [JsonPropertyName("оружие")]
        public WeaponProficiency Weapons { get; set; } = new();
    }

    public class ArmorProficiency
    {
        [JsonPropertyName("легкие")]
        public bool Light { get; set; }

        [JsonPropertyName("средние")]
        public bool Medium { get; set; }

        [JsonPropertyName("тяжелые")]
        public bool Heavy { get; set; }

        [JsonPropertyName("щиты")]
        public bool Shields { get; set; }
    }

    public class WeaponProficiency
    {
        [JsonPropertyName("простое")]
        public bool Simple { get; set; }

        [JsonPropertyName("воинское")]
        public bool Martial { get; set; }

        [JsonPropertyName("другое")]
        public List<string> Other { get; set; } = new();
    }

    public class Abilities
    {
        [JsonPropertyName("способностикласса")]
        public List<Ability> ClassAbilities { get; set; } = new();

        [JsonPropertyName("видовыеспособности")]
        public List<Ability> SpeciesAbilities { get; set; } = new();

        [JsonPropertyName("черты")]
        public List<Ability> Feats { get; set; } = new();
    }

    public class Ability
    {
        [JsonPropertyName("название")]
        public string Name { get; set; } = "";

        [JsonPropertyName("описание")]
        public string Description { get; set; } = "";
    }

    public class Needs
    {
        [JsonPropertyName("сколькоест")]
        public DailyNeed Food { get; set; } = new();

        [JsonPropertyName("сколькопьет")]
        public DailyNeed Water { get; set; } = new();

        [JsonPropertyName("грузоподъемность")]
        public CarryingCapacity CarryingCapacity { get; set; } = new();
    }

    public class DailyNeed
    {
        [JsonPropertyName("размер")]
        public string Size { get; set; } = "средний";

        [JsonPropertyName("вдень")]
        public string PerDay { get; set; } = "";
    }

    public class CarryingCapacity
    {
        [JsonPropertyName("максимум")]
        public int Max { get; set; }

        [JsonPropertyName("единица")]
        public string Unit { get; set; } = "кг";
    }

    public class Movement
    {
        [JsonPropertyName("кмзаход")]
        public int KmPerTurn { get; set; }
    }

    public class Wealth
    {
        [JsonPropertyName("монеты")]
        public Coins Coins { get; set; } = new();
    }

    public class Coins
    {
        [JsonPropertyName("медные")]
        public int Copper { get; set; }

        [JsonPropertyName("серебряные")]
        public int Silver { get; set; }

        [JsonPropertyName("золотые")]
        public int Gold { get; set; }

        [JsonPropertyName("платиновые")]
        public int Platinum { get; set; }
    }

    public class InventoryItem
    {
        [JsonPropertyName("название")]
        public string Name { get; set; } = "";

        [JsonPropertyName("тип")]
        public string Type { get; set; } = "";

        [JsonPropertyName("урон")]
        public string? Damage { get; set; }

        [JsonPropertyName("вес")]
        public double? Weight { get; set; }

        [JsonPropertyName("бонусКД")]
        public int? ArmorClassBonus { get; set; }

        [JsonPropertyName("КД")]
        public int? ArmorClass { get; set; }

        [JsonPropertyName("количество")]
        public int? Quantity { get; set; }
    }

    public class EquippedGear
    {
        [JsonPropertyName("голова")]
        public string? Head { get; set; }

        [JsonPropertyName("тело")]
        public string? Body { get; set; }

        [JsonPropertyName("руки")]
        public string? Hands { get; set; }

        [JsonPropertyName("ноги")]
        public string? Legs { get; set; }

        [JsonPropertyName("обувь")]
        public string? Feet { get; set; }

        [JsonPropertyName("основнаярука")]
        public string? MainHand { get; set; }

        [JsonPropertyName("вторая_рука")]
        public string? OffHand { get; set; }

        [JsonPropertyName("амулет")]
        public string? Amulet { get; set; }

        [JsonPropertyName("кольцо_1")]
        public string? Ring1 { get; set; }

        [JsonPropertyName("кольцо_2")]
        public string? Ring2 { get; set; }
    }

    public class CombatStats
    {
        [JsonPropertyName("классдоспеха")]
        public int ArmorClass { get; set; }

        [JsonPropertyName("бонусмастерства")]
        public int ProficiencyBonus { get; set; }

        [JsonPropertyName("атаки")]
        public List<Attack> Attacks { get; set; } = new();
    }

    public class Attack
    {
        [JsonPropertyName("название")]
        public string Name { get; set; } = "";

        [JsonPropertyName("бросок")]
        public string Roll { get; set; } = "";

        [JsonPropertyName("урон")]
        public string Damage { get; set; } = "";
    }
}