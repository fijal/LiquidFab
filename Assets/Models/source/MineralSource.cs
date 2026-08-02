using UnityEngine;

public class MineralSource : MonoBehaviour
{
    public Terrain terrain;

    const float INTERVAL = 3.0f;
    float timer;
    void Start()
    {
        timer = INTERVAL;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            timer = INTERVAL;
            var r = Random.Range(0f, 1f);
            var cur = transform.position;
            cur = new Vector3(cur.x, terrain.heightFloat(cur.x / Terrain.SCALE, cur.z / Terrain.SCALE), cur.z);
            if (r < 0.1f)
            {
                terrain.spawnFloater(cur, ItemType.Iron);
            } else if (r < 0.2f)
            {
                terrain.spawnFloater(cur, ItemType.Copper);
            }
            else
            {
                terrain.spawnFloater(cur, ItemType.Rock);
            }
        }
    }
}
