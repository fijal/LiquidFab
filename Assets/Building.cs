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

public class Building : MonoBehaviour
{
    [HideInInspector] public Terrain terrain;
    public GameObject spawnPoint, pickupPoint;
    public BuildingKind kind;
    public Receipe[] receipes;
    public bool[] receipesEnabled;
    public Dictionary<ItemType, int> inventory;
    int[] productsGathered;
    [HideInInspector] public ProductionState state = ProductionState.idle;
    float buildTimer = 0;

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

    public void receipeProgress()
    {
        if (kind == BuildingKind.waterWheel)
        {
            // XXX hack until we know how to do it better XXX
            var force = 0.3f;
            var x = (int)(transform.position.x / Terrain.SCALE)      ;
            var y = (int)(transform.position.z / Terrain.SCALE);
            terrain.water.waterFlowX[x + y * (Terrain.TERRAIN_SIZE + 1)] = Mathf.Cos(Mathf.Deg2Rad * transform.localRotation.eulerAngles.y) * force;
            terrain.water.waterFlowY[x + y * Terrain.TERRAIN_SIZE] = -Mathf.Sin(Mathf.Deg2Rad * transform.localRotation.eulerAngles.y) * force;
        }
        if (productsGathered == null)
            return; // non producing building
        /*if (state != ProductionState.producing)
        {
            if (productsGathered.Length == 0)
            {
                state = ProductionState.starting;
                buildTimer = selectedReceipe.time;
            }
            else
            {
                var c = Physics.OverlapBox(pickupPoint.transform.position, new Vector3(0.3f, 0.3f, 0.3f), pickupPoint.transform.rotation,
                                           ColliderLayers.Floaters);
                for (int i = 0; i < c.Length; ++i)
                {
                    var tp = c[i].GetComponent<Floater>().tp;
                    for (int j = 0; j < productsGathered.Length; ++j)
                        if (productsGathered[j] < selectedReceipe.inputCounts[j] && selectedReceipe.inputs[j].tp == tp)
                        {
                            productsGathered[j]++;
                            terrain.removeFloater(c[i].gameObject);
                        }
                }
                // check if we have all
                var all = true;
                for (int j = 0; j < productsGathered.Length; ++j)
                    if (productsGathered[j] < selectedReceipe.inputCounts[j])
                        all = false;
                if (all)
                {
                    buildTimer = selectedReceipe.time;
                    state = ProductionState.starting;
                }
            }
        }*/
    }

    public void FixedUpdate()
    {
        if (state == ProductionState.producing)
        {
            buildTimer -= Time.fixedDeltaTime;
            if (buildTimer < 0)
            {
                state = ProductionState.stopping;
                //var item = selectedReceipe.output.GetComponent<Item>();
                //terrain.spawnFloater(spawnPoint.transform.position, item.prefab, item.tp);
                //setReceipe(selectedReceipe);
                receipeProgress(); // run one gathering of resources
            }
        }
    }
}
