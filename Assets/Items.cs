using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Items : MonoBehaviour
{
    public Dictionary<ItemType, Item> items;
   
    public void Start()
    {
        items = new Dictionary<ItemType, Item>();
        for (int i = 0; i < transform.childCount; i++)
        {
            var item = transform.GetChild(i).gameObject.GetComponent<Item>();
            items[item.tp] = item;
        }
    }
}
