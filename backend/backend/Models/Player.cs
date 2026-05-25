namespace backend.Models
{
    public class Player
    {
        public Character Character { get; set; } = new();
        public Resources Resources { get; set; } = new();
        public Attributes Attributes { get; set; } = new();
        public EquipmentProficiency EquipmentProficiency { get; set; } = new();
        public Abilities Abilities { get; set; } = new();
        public Needs Needs { get; set; } = new();
        public Movement Movement { get; set; } = new();
        public Wealth Wealth { get; set; } = new();

        public List<string> Inventory { get; set; } = new();
        public EquippedGear EquippedGear { get; set; } = new();
    }

    public class Character
    {
        public string Name { get; set; } = "";
        public string Background { get; set; } = "";
        public string Species { get; set; } = "";
        public string Class { get; set; } = "";
        public string Subclass { get; set; } = "";
        public int Level { get; set; }
        public int Experience { get; set; }
    }

    public class Resources
    {
        public Stat Hp { get; set; } = new();
        public Stat Mana { get; set; } = new();
        public Stat ActionPoints { get; set; } = new();
        public Conditions Conditions { get; set; } = new();
    }

    public class Stat
    {
        public int Max { get; set; }
        public int Current { get; set; }
    }

    public class Conditions
    {
        public List<Condition> Active { get; set; } = new();
    }

    public class Condition
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";

        public int? DurationTurns { get; set; }
        public int? Power { get; set; }

        public bool IsPermanent { get; set; }
    }

    public class Attributes
    {
        public int Strength { get; set; }
        public int Intelligence { get; set; }
        public int Dexterity { get; set; }
        public int Wisdom { get; set; }
        public int Constitution { get; set; }
        public int Charisma { get; set; }
        public int Initiative { get; set; }
        public int Speed { get; set; }
        public int Perception { get; set; }
    }

    public class EquipmentProficiency
    {
        public ArmorProficiency Armor { get; set; } = new();
        public WeaponProficiency Weapons { get; set; } = new();
    }

    public class ArmorProficiency
    {
        public bool Light { get; set; }
        public bool Medium { get; set; }
        public bool Heavy { get; set; }
        public bool Shields { get; set; }
    }

    public class WeaponProficiency
    {
        public bool Simple { get; set; }
        public bool Martial { get; set; }
        public List<string> Other { get; set; } = new();
    }

    public class Abilities
    {
        public List<Ability> ClassAbilities { get; set; } = new();
        public List<Ability> SpeciesAbilities { get; set; } = new();
        public List<Ability> Feats { get; set; } = new();
    }

    public class Ability
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class Needs
    {
        public DailyNeed Food { get; set; } = new();
        public DailyNeed Water { get; set; } = new();
        public CarryingCapacity CarryingCapacity { get; set; } = new();
    }

    public class DailyNeed
    {
        public string Size { get; set; } = "medium";
        public string PerDay { get; set; } = "";
    }

    public class CarryingCapacity
    {
        public int Max { get; set; }
        public string Unit { get; set; } = "kg";
    }

    public class Movement
    {
        public int KmPerTurn { get; set; }
    }

    public class Wealth
    {
        public Coins Coins { get; set; } = new();
    }

    public class Coins
    {
        public int Copper { get; set; }
        public int Silver { get; set; }
        public int Gold { get; set; }
        public int Platinum { get; set; }
    }

    public class EquippedGear
    {
        public string? Head { get; set; }
        public string? Body { get; set; }
        public string? Hands { get; set; }
        public string? Legs { get; set; }
        public string? Feet { get; set; }
        public string? MainHand { get; set; }
        public string? OffHand { get; set; }
        public string? Amulet { get; set; }
        public string? Ring1 { get; set; }
        public string? Ring2 { get; set; }
    }
}