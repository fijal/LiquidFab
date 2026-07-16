using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinerTool : BuildingFreePlacement
{
    public MinerTool(BuildingSpec spec) : base(spec)
    {
    }

    public override void clickTerrain(GameObject highlight, Terrain terrain, Vector3 hitPoint)
    {
        terrain.spawnMiner(hitPoint, highlight.transform.rotation);
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
