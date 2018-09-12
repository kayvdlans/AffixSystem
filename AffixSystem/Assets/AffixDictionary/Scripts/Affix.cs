using System;
using System.Collections;
using UnityEngine;


//TODO: Get Type, Modifier and ModifierType from .json arrays, 
//      so people will be able to add new ones through the editorwindow.
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

    /// <summary>
    /// Turns the item level into a bit array, so it's easier to sort it. 
    /// Also flips the bit array, since it usually will return it backwards.
    /// </summary>
    /// <returns>Flipped BitArray representing the item level of the affix.</returns>
    public BitArray GetItemLevelAsBitArray()
    {
        BitArray b = new BitArray(new byte[] { (byte)item_level });
        for (int i = 0; i < Mathf.Floor(b.Length / 2); i++)
        {
            bool ii = b[i];
            bool ij = b[b.Length - 1 - i];
            b[i] = ij;
            b[b.Length - 1 - i] = ii;
        }

        return b;
    }

    /// <summary>
    /// A compare function which can be used to sort a list based on the types, modifiers, item level, etc. 
    /// </summary>
    /// <param name="other">The Affix to compare to </param>
    /// <returns></returns>
    public int CompareTo(Affix other)
    {
        BitArray xb = GetItemLevelAsBitArray();
        BitArray yb = other.GetItemLevelAsBitArray();
        string x_item_level = "";
        string y_item_level = "";

        for (int i = 0; i < xb.Length; i++)
        {
            x_item_level += xb[i] ? 1 : 0;
            y_item_level += yb[i] ? 1 : 0;
        }

        string x = item_type.ToString() + "_" + type.ToString()
            + "_" + modifier_type.ToString() + "_" + modifier.ToString()
            + "_" + x_item_level;
        string y = other.item_type.ToString() + "_" + other.type.ToString()
            + "_" + other.modifier_type.ToString() + "_" + other.modifier.ToString()
            + "_" + y_item_level;

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
