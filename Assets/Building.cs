using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    public Terrain terrain;
    public GameObject spawnPoint, pickupPoint;
    public Receipe selectedReceipe;
    int[] productsGathered;
    public bool producing = false;
    float buildTimer = 0;

    public void receipeProgress()
    {
        if (!producing)
        {
            if (productsGathered.Length == 0)
            {
                producing = true;
                buildTimer = selectedReceipe.time;
            }
            else
            {

            }
        }
    }

    public void setReceipe(Receipe receipe)
    {
        selectedReceipe = receipe;
        productsGathered = new int[receipe.inputs.Length];
    }

    public void FixedUpdate()
    {
        if (producing)
        {
            buildTimer -= Time.fixedDeltaTime;
            if (buildTimer < 0)
            {
                producing = false;
                terrain.spawnFloater(gameObject, selectedReceipe.output.GetComponent<Item>().prefab);
                receipeProgress(); // run one gathering of resources
            }
        }
    }
}
