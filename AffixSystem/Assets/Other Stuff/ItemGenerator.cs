using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemGenerator : MonoBehaviour
{
    enum Rarity
    {
        Normal,
        Magic,
        Rare
    }

    private const int MAX_GENERATION_LOOPS = 100;

    [SerializeField] private bool m_RandomRarity;
    [SerializeField] private Rarity m_Rarity;
    [SerializeField] private Item.Type m_ItemType;
    [SerializeField] private int m_ItemLevel;

    private List<Affix> m_Affixes;

    private string[] m_PrefixNames = 
    {
        "Agony", "Apocalypse", "Armageddon", "Beast", "Behemoth", "Blight",
        "Blood", "Bramble", "Brimstone", "Brood", "Carrion", "Cataclysm",
        "Chimeric", "Corpse", "Corruption", "Damnation", "Death", "Demon",
        "Dire", "Dragon", "Dread", "Doom", "Dusk", "Eagle", "Empyrean", "Fate",
        "Foe", "Gale", "Ghoul", "Gloom", "Glyph", "Golem", "Grim", "Hate",
        "Havoc", "Honour", "Horror", "Hypnotic", "Kraken", "Loath", "Maelstrom",
        "Mind", "Miracle", "Morbid", "Oblivion", "Onslaught", "Pain",
        "Pandemonium", "Phoenix", "Plague", "Rage", "Rapture", "Rune", "Skull",
        "Sol", "Soul", "Sorrow", "Spirit", "Storm", "Tempest", "Torment",
        "Vengeance", "Victory", "Viper", "Vortex", "Woe", "Wrath"
    };

    //Only body armour names
    private string[] m_SuffixNames =
    {
        "Carapece", "Cloack", "Coat", "Curtain", "Guardian", "Hide", "Jack",
        "Keep", "Mantle", "Pelt", "Salvation", "Sanctuary", "Shell", "Shelter",
        "Shroud", "Skin", "Suit", "Veil", "Ward", "Wrap"
    };

    //logic is for both prefixes and suffixes
    private Dictionary<Rarity, uint> RarityToMaxAffix = new Dictionary<Rarity, uint>()
    {
        { Rarity.Normal, 0 },
        { Rarity.Magic, 1 },
        { Rarity.Rare, 3 }
    };

    //TODO: Actually do something with the generation of the affixes;
    private void Generate()
    {
        List<Affix> dict = GameObject.FindGameObjectWithTag("AffixDictionary").GetComponent<AffixDictionary>().GetAffixesByItemType(m_ItemType);
        dict = GetValidAffixes(dict, m_ItemLevel);
        m_Affixes = new List<Affix>();

        int prefixes = 0;
        int suffixes = 0;
        bool creationFailed = false;

        if (m_RandomRarity)
        {
            m_Rarity = (Rarity) Random.Range(0, 3);
        }

        uint minAffixes = RarityToMaxAffix[m_Rarity];

        while (prefixes + suffixes < minAffixes)
        {
            switch (m_Rarity)
            {
                case Rarity.Normal:
                    break;
                case Rarity.Magic:
                        prefixes = Random.Range(0, 2);
                        suffixes = Random.Range(0, 2);
                    break;
                case Rarity.Rare:
                        prefixes = Random.Range(1, 4);
                        suffixes = Random.Range(1, 4);
                    break;
                default:
                    Debug.Log("You're a moron, this shouldn't even be able to happen?!");
                    break;
            }
        }
        
        int currentPrefixes = 0;
        int currentSuffixes = 0;
        int generationLoops = 0;
        while ((currentPrefixes != prefixes || currentSuffixes != suffixes) && generationLoops < MAX_GENERATION_LOOPS)
        {
            int i = Random.Range(0, dict.Count);

            if (dict[i].type == Affix.Type.Prefix)
            {
                if (currentPrefixes < prefixes )
                {
                    AddAffixIfModifierNotAdded(dict[i], ref currentPrefixes);
                }
            }
            else if (dict[i].type == Affix.Type.Suffix)
            {
                if (currentSuffixes < suffixes)
                {
                    AddAffixIfModifierNotAdded(dict[i], ref currentSuffixes);
                }
            }

            generationLoops++;
        }

        if (generationLoops >= MAX_GENERATION_LOOPS)
        {
            creationFailed = true;
            Debug.Log("Generation of item affixes failed @ line 95 in ItemGenerator.cs"); /* Most likely due to lack of affixes, although still nice to know it failed */
            m_Affixes.Clear();
        }

        List<Affix> preffies = new List<Affix>();
        List<Affix> suffies = new List<Affix>();

        for (int i = 0; i < m_Affixes.Count; i++)
        {
            if (m_Affixes[i].type == Affix.Type.Prefix)
            {
                preffies.Add(m_Affixes[i]);
            }
            else
            {
                suffies.Add(m_Affixes[i]);
            }
        }

        if (!creationFailed)
        {
            GameObject ob = GameObject.FindGameObjectWithTag("ItemFrame");
            string name = "";
            for(int i = 0; i < 3; i++)
            {
                if (preffies.Count > i)
                {
                    ob.transform.GetChild(i).GetComponent<Text>().text =
                        "+" + Random.Range(preffies[i].min_amount, preffies[i].max_amount + 1) + (preffies[i].modifier_type == Affix.ModifierType.Flat ? " " : "% ") +
                        "to " + preffies[i].modifier.ToString();
                    if (m_Rarity == Rarity.Magic)  
                        name += preffies[i].name + " ";
                }
                else
                {
                    ob.transform.GetChild(i).GetComponent<Text>().text = "";
                }
            }
            if (m_Rarity == Rarity.Rare)
                name += m_PrefixNames[Random.Range(0, m_PrefixNames.Length)] + " " + m_SuffixNames[Random.Range(0, m_SuffixNames.Length)] + " ";
            name += m_ItemType.ToString() + " ";
            for(int i = 0; i < 3; i++)
            {
                if (suffies.Count > i)
                {
                    ob.transform.GetChild(i + 3).GetComponent<Text>().text =
                        "+" + Random.Range(suffies[i].min_amount, suffies[i].max_amount + 1) + (suffies[i].modifier_type == Affix.ModifierType.Flat ? " " : "% ") +
                        "to " + suffies[i].modifier.ToString();
                    if (m_Rarity == Rarity.Magic)
                        name += suffies[i].name + " ";
                }
                else
                {
                    ob.transform.GetChild(i + 3).GetComponent<Text>().text = "";
                }
            }

            ob.transform.GetChild(6).GetComponent<Text>().text = name;

            
        }
    }

    private List<Affix> GetValidAffixes(List<Affix> affixes, int item_level)
    {
        List<Affix> af = new List<Affix>();

        for (int i = 0; i < affixes.Count; i++)
            if (affixes[i].item_level <= item_level)
                af.Add(affixes[i]);

        return af;
    }

    private void AddAffixIfModifierNotAdded(Affix affix, ref int amount)
    {
        foreach (Affix a in m_Affixes)
        {
            if (affix.type == a.type && 
                affix.modifier == a.modifier && 
                affix.modifier_type == a.modifier_type)
            {
                return;
            }
        }

        m_Affixes.Add(affix);
        amount++;
    }

    private void Start()
    {
        Generate();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Generate();
            //generate new baby boi
        }
    }
}
