using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    [HideInInspector] public Terrain terrain;
    public GameObject spawnPoint, pickupPoint;
    public Receipe selectedReceipe;
    int[] productsGathered;
    [HideInInspector] public bool producing = false;
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
                var c = Physics.OverlapBox(pickupPoint.transform.position, new Vector3(0.5f, 0.5f, 0.5f), pickupPoint.transform.rotation, 1 << 7);
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
                    producing = true;
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
                var item = selectedReceipe.output.GetComponent<Item>();
                terrain.spawnFloater(gameObject, item.prefab, item.tp);
                setReceipe(selectedReceipe);
                receipeProgress(); // run one gathering of resources
            }
        }
    }
}
