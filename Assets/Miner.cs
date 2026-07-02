using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Miner : MonoBehaviour
{
    public GameObject ironPrefab;
    public Terrain terrain;
    float nextSpawn;

    private void Start()
    {
        nextSpawn = 1.0f;
    }

    void Update()
    {
        nextSpawn -= Time.deltaTime;
        if (nextSpawn <= 0)
        {
            nextSpawn = 1.0f;
            terrain.spawnFloater(gameObject, ironPrefab);
        }
    }
}
