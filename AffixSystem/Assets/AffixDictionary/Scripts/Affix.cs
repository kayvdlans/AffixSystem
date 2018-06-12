using System;

[Serializable]
public struct Affix
{
    public enum Type
    {
        Implicit,
        Prefix,
        Suffix
    }

    public enum Modifier
    {
        Health,
        Mana,
        ResistanceCold,
        ResistanceFire,
        ResistanceLightning,
        ResistanceChaos,
        Strength,
        Dexterity,
        Intelligence
    }

    public enum ModifierType
    {
        Flat,
        Percentage
    }

    public string name;
    public int item_level;
    public int min_amount;
    public int max_amount;
    public Type type;
    public Modifier modifier;
    public ModifierType modifier_type;
    public Item.Type item_type;
}
