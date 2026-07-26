using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Iron = 1,
    IronPlate = 2,
    Gear = 3,
    Copper = 4,
    Rock = 5
}

public class Item : MonoBehaviour
{
    public GameObject prefab;
    public ItemType tp;
}
