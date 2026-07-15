using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Iron = 1,
    IronPlate = 2,
    Gear = 3,
}

public class Item : MonoBehaviour
{
    public GameObject prefab;
    public ItemType tp;
}
