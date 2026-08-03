using System.Collections.Generic;
using UnityEngine;

public class Construction : MonoBehaviour
{
    public Dictionary<ItemType, int> inventory, cost;
    public BuildingKind kind;

    public void populateFromSpec(BuildingSpec spec)
    {
        cost = new Dictionary<ItemType, int>();
        inventory = new Dictionary<ItemType, int>();
        foreach (var item in spec.buildingCost)
        {
            cost[item.Key] = item.Value;
            inventory[item.Key] = 0;
        }
    }

    public bool isValidIngredient(ItemType tp)
    {
        return inventory.ContainsKey(tp) && inventory[tp] < cost[tp];
    }

    public void maybeFinishConstruction(Terrain terrain)
    {
        foreach (var item in inventory)
            if (item.Value < cost[item.Key])
                return;
        // XXX ARGH XXX
        var spec = terrain.controls.tools.buildingMapping[kind] as BuildingFreePlacement;
        terrain.spawnBuilding(spec.spec.prefab, transform.position, transform.rotation, spec.spec);
        terrain.removeBuilding(gameObject);
    }

    public void addInventory(ItemType tp, Terrain terrain)
    {
        inventory[tp] += 1;
        // we cheating here cause the dialog box can't be open just yet, but notify UI here at some stage
        maybeFinishConstruction(terrain);
    }
}
