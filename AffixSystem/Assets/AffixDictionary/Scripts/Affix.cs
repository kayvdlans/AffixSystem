using System;
using System.Collections;

[Serializable]
public struct Affix : IComparable<Affix>
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

    public BitArray GetItemLevelAsBitArray()
    {
        return new BitArray(new byte[] { (byte)item_level });
    }

    public int CompareTo(Affix other)
    {
        string x = item_type.ToString() + "_" + type.ToString() 
            + "_" + modifier_type.ToString() + "_" + modifier.ToString() 
            + "_" + GetItemLevelAsBitArray().ToString();
        string y = other.item_type.ToString() + "_" + other.type.ToString() 
            + "_" + other.modifier_type.ToString() + "_" + other.modifier.ToString()
            + "_" + other.GetItemLevelAsBitArray().ToString();

        return x.CompareTo(y);
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
