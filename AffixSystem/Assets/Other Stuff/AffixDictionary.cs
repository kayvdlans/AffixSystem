using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class AffixDictionary
{
    [SerializeField]
    private List<Affix> affixes;

    public List<Affix> GetAffixesByItemType(Item.Type itemType)
    {
        List<Affix> sortedAffixes = new List<Affix>();

        foreach (Affix affix in affixes)
        {
            if (affix.item_type == itemType)
            {
                sortedAffixes.Add(affix);
            }
        }

        return sortedAffixes;
    }

    public List<Affix> Affixes { get { return affixes; } }
}
