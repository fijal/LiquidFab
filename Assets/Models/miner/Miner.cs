using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinerTool : BuildingFreePlacement
{
    public MinerTool(BuildingSpec spec) : base(spec)
    {
    }
}

public class Miner : MonoBehaviour
{
    public GameObject movingPart;
    float pos = 0;
    ParticleSystem smoke;

    void Start()
    {
        smoke = transform.Find("Smoke").gameObject.GetComponent<ParticleSystem>();
    }

    private void FixedUpdate()
    {
        var terrain = GetComponent<Building>().terrain;
        if (terrain.water.waterLevelFloat(transform.position.x / Terrain.SCALE, transform.position.z / Terrain.SCALE) > 0.1f)
        {
            int x = (int)(transform.position.x / Terrain.SCALE);
            int y = (int)(transform.position.z / Terrain.SCALE);
            terrain.water.mud[x + y * Terrain.TERRAIN_SIZE] = 0.5f;
            //Debug.Log("emitting mud");
        }
    }

    void Update()
    {
        var state = GetComponent<Building>().state;
        if (state == ProductionState.producing)
        {
            pos += Time.deltaTime;
            var c = movingPart.transform.localPosition;
            movingPart.transform.localPosition = new Vector3(c.x, 0.1f * (Mathf.Sin(pos / 3f) - 1), c.z);
        } else if (state == ProductionState.starting)
        {
            smoke.Play();
            GetComponent<Building>().state = ProductionState.producing;
        }
        else if (state == ProductionState.stopping)
        {
            smoke.Stop();
            GetComponent<Building>().state = ProductionState.idle;
        }
    }
}
