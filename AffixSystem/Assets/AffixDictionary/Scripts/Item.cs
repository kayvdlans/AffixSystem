using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public enum Type
    {
        BodyArmour,
        Helmet,
        Gloves,
        Boots,
        Weapon,
        Shield
    }

    public int ItemLevel { get; set; }
}
