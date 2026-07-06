using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Miner : MonoBehaviour
{
    public GameObject ironPrefab;
    public GameObject movingPart;
    
    float nextSpawn;
    float pos = 0;

    private void Start()
    {
        nextSpawn = 3.0f;
    }

    void Update()
    {
        nextSpawn -= Time.deltaTime;
        if (nextSpawn <= 0)
        {
            nextSpawn = 3.0f;
            GetComponent<Building>().terrain.spawnFloater(gameObject, ironPrefab);
        }
        pos += Time.fixedDeltaTime;
        var c = movingPart.transform.localPosition;
        movingPart.transform.localPosition = new Vector3(c.x, 0.05f * (Mathf.Sin(pos / 3f) - 1), c.z);
    }
}
