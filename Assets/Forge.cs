using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForgeBehaviour : BuildingFreePlacement
{
    public ForgeBehaviour(BuildingSpec spec) : base(spec)
    {
    }

    public override void clickTerrain(GameObject highlight, Terrain terrain, Vector3 hitPoint)
    {
        terrain.spawnForge(hitPoint, highlight.transform.rotation);
    }
}


public class Forge : MonoBehaviour
{
    public GameObject movingPart;
    ParticleSystem smoke;
    float pos = 0;

    public float timer;

    void Start()
    {
        smoke = transform.Find("Smoke").gameObject.GetComponent<ParticleSystem>();
    }

    void FixedUpdate()
    {
        var state = GetComponent<Building>().state;
        if (state == ProductionState.producing)
        {
            pos += Time.fixedDeltaTime;
            var c = movingPart.transform.localPosition;
            movingPart.transform.localPosition = new Vector3(c.x, 0.05f * (Mathf.Sin(pos / 1f) + 1.2f), c.z);
        } else if (state == ProductionState.starting)
        {
            smoke.Play();
            GetComponent<Building>().state = ProductionState.producing;
        } else if (state == ProductionState.stopping)
        {
            smoke.Stop();
            GetComponent<Building>().state = ProductionState.idle;
        }
    }
}
