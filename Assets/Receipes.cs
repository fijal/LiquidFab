using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Receipe
{
    public Item[] inputs;
    public int[] inputCounts;
    public Item output;
    public int outputCount;
    public float time;

    public Receipe (Item[] inputs, int[] inputCounts, Item output, int outputCount, float time)
    {
        this.inputs = inputs;
        this.inputCounts = inputCounts;
        this.output = output;
        this.outputCount = outputCount;
        this.time = time;
    }
}

public class Receipes : MonoBehaviour
{
    public Item[] items;
    public Receipe[] receipes;

    public void Start()
    {
        items = new Item[transform.childCount + 1];
        for (int i = 0; i < transform.childCount; i++)
        {
            var item = transform.GetChild(i).gameObject.GetComponent<Item>();
            items[(int)item.tp] = item;
        }

        receipes = new Receipe[3];
        var iron = items[(int)ItemType.Iron].GetComponent<Item>();
        var ironPlate = items[(int)ItemType.IronPlate].GetComponent<Item>();
        var gear = items[(int)ItemType.Gear].GetComponent<Item>();

        receipes[0] = new Receipe(new Item[]{ }, new int[] { }, iron, 1, 3.0f);
        receipes[1] = new Receipe(new Item[] { iron }, new int[] { 1 }, ironPlate, 1, 3.0f);
        receipes[2] = new Receipe(new Item[] { ironPlate }, new int[] { 2 }, gear, 1, 3.0f);
    }
}
