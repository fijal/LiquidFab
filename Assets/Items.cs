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
            var go = transform.GetChild(i).gameObject;
            var item = go.GetComponent<Item>();
            items[item.tp] = item;

            // check if the items are ok
            //Debug.Log($"checking {item.prefab}");
            Debug.Assert(item.tp != ItemType.None);
            Debug.Assert(1 << item.prefab.layer == ColliderLayers.Floaters);
            // item.prefab.tp can be either checked or fixed
            Debug.Assert(item.prefab.GetComponent<Floater>().tp == item.tp);
            Debug.Assert(item.prefab.GetComponent<Rigidbody>().excludeLayers == (ColliderLayers.BuildingsNoFloater | ColliderLayers.Water));
        }
    }
}
