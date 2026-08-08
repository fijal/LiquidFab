using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ProductionState
{
    idle = 1,
    starting = 2,
    stopping = 3,
    producing = 4
}

public enum BuildingKind
{
    miner = 1,
    forge = 2,
    assembler = 3,
    waterWheel = 4,
    fence = 5,
    waterPump = 6,
    mainBase = 7
}

public class BuildingHelper
{
    public static BuildingKind getKind(GameObject go)
    {
        if (go.GetComponent<Building>() != null)
            return go.GetComponent<Building>().kind;
        return go.GetComponent<Construction>().kind;
    }
}

public class Building : MonoBehaviour
{
    [HideInInspector] public Terrain terrain;
    public GameObject spawnPoint, pickupPoint;
    public BuildingKind kind;
    public Receipe[] receipes;
    public bool[] receipesEnabled;
    public Dictionary<ItemType, int> inventory;
    [HideInInspector] public ProductionState state = ProductionState.idle;
    float buildTimer = 0;
    int currentReceipe = -1;
    [HideInInspector] public BuildingDetails ui;

    const int MAX_INGREDIENTS = 10;

    public void populateFromSpec(BuildingSpec spec)
    {
        receipes = spec.receipes;
        receipesEnabled = new bool[receipes.Length];
        inventory = new Dictionary<ItemType, int>();
        for (int i = 0; i < receipes.Length; i++)
        {
            receipesEnabled[i] = true;
            foreach (var item in receipes[i].inputs.Keys)
                inventory[item] = 0;
        }
    }

    void checkNextReceipe()
    {
        for (int i = 0; i < receipes.Length; ++i)
        {
            if (!receipesEnabled[i])
                continue;
            var all = true;
            foreach (var ing in receipes[i].inputs)
                if (inventory[ing.Key] < ing.Value)
                {
                    all = false;
                    break;
                }
            if (all)
            {
                currentReceipe = i;
                foreach (var ing in receipes[i].inputs)
                    inventory[ing.Key] -= ing.Value;
                if (ui != null)
                    ui.notifyInventoryChange(inventory);
                buildTimer = receipes[i].time;
                if (state != ProductionState.producing)
                    state = ProductionState.starting;
                break;
            }
        }
    }

    public void receipeProgress()
    {
        if (receipes == null)
            return; // non producing building
        if (state != ProductionState.producing)
            checkNextReceipe();
    }

    public bool isValidIngredient(ItemType tp)
    {
        if (inventory == null)
            return false;
        return inventory.ContainsKey(tp) && inventory[tp] < MAX_INGREDIENTS;
    }

    public void addInventory(ItemType tp)
    {
        inventory[tp] += 1;
    }

    void checkForIngredients()
    {
        var c = Physics.OverlapBox(pickupPoint.transform.position, new Vector3(0.3f, 0.3f, 0.3f), pickupPoint.transform.rotation,
                                           ColliderLayers.Floaters);
        for (int i = 0; i < c.Length; ++i)
        {
            var tp = c[i].GetComponent<Floater>().tp;
            Debug.Log($"position {transform.position}");
            Debug.Log($"pickup point {pickupPoint.transform.position}");
            Debug.Log($"spawn point {spawnPoint.transform.position}");
            Debug.Log($"floater {c[i].transform.position}");
            Debug.Log(tp);
            if (inventory.ContainsKey(tp) && inventory[tp] < MAX_INGREDIENTS)
            {
                Destroy(c[i].gameObject);
                inventory[tp] += 1;
                if (ui != null)
                    ui.notifyInventoryChange(inventory);
            }
        }
    }
    public void FixedUpdate()
    {
        if (kind == BuildingKind.waterWheel)
        {
            // XXX hack until we know how to do it better XXX
            var force = 0.3f;
            var x = (int)(transform.position.x / Terrain.SCALE);
            var y = (int)(transform.position.z / Terrain.SCALE);
            terrain.water.waterFlowX[x + y * (Terrain.TERRAIN_SIZE + 1)] = Mathf.Cos(Mathf.Deg2Rad * transform.localRotation.eulerAngles.y) * force;
            terrain.water.waterFlowY[x + y * Terrain.TERRAIN_SIZE] = -Mathf.Sin(Mathf.Deg2Rad * transform.localRotation.eulerAngles.y) * force;
        }
        if (receipes == null)
            return;
        checkForIngredients();
        receipeProgress();
        if (state == ProductionState.producing)
        {
            buildTimer -= Time.fixedDeltaTime;
            if (ui)
                ui.notifyReceipeProgress(currentReceipe, 1.0f - buildTimer / receipes[currentReceipe].time);
            if (buildTimer < 0)
            {
                state = ProductionState.stopping;
                var items = receipes[currentReceipe].outputs;
                foreach (var itemTp in items) {
                    for (var i = 0; i < itemTp.Value; i++)
                        terrain.spawnFloater(spawnPoint.transform.position, itemTp.Key);
                }
                if (ui)
                    ui.notifyReceipeProgress(currentReceipe, 0);
                checkNextReceipe();
            }
        }
    }
}
