using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Forge : MonoBehaviour
{
    public Terrain terrain;
    public GameObject movingPart, spawnPoint, pickupPoint;
    float pos = 0;

    public bool producing = false;
    public float timer;

    void FixedUpdate()
    {
        pos += Time.fixedDeltaTime;
        var c = movingPart.transform.localPosition;
        movingPart.transform.localPosition = new Vector3(c.x, 0.05f * (Mathf.Sin(pos / 3f) + 1.2f), c.z);

        if (producing)
        {
            timer -= Time.fixedDeltaTime;
            if (timer <= 0)
            {
                producing = false;
                terrain.spawnFloater(gameObject, terrain.ironPlatePrefab);
            }
        }
    }
}
