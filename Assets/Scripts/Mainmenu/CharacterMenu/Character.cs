[System.Serializable]
public class Character
{
    public int id;           // Unique identifier for the character
    public string name;      // Name of the character
    public int level;        // Current level of the character
    public int price;        // Price to unlock the character
    public bool isUnlocked;  // Whether the character is unlocked

    public Character(int id, string name, int level, int price, bool isUnlocked)
    {
        this.id = id;
        this.name = name;
        this.level = level;
        this.price = price;
        this.isUnlocked = isUnlocked;
    }
}
