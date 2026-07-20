using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssemblerTool : BuildingFreePlacement
{
    public AssemblerTool(BuildingSpec spec) : base(spec)
    {
    }

    public override void clickTerrain(GameObject highlight, Terrain terrain, Vector3 hitPoint)
    {
        if (isLegalPlacement(highlight, terrain, hitPoint))
            terrain.spawnAssembler(hitPoint, highlight.transform.rotation);
    }
}

public class Assembler : MonoBehaviour
{
    public GameObject gear1, gear2;
    ParticleSystem smoke;
    
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
            gear1.transform.rotation *= Quaternion.Euler(0, 360 * Time.deltaTime, 0);
            gear2.transform.rotation *= Quaternion.Euler(0, 360 * Time.deltaTime, 0);
            //pos += Time.fixedDeltaTime;
            //var c = movingPart.transform.localPosition;
            //movingPart.transform.localPosition = new Vector3(c.x, 0.05f * (Mathf.Sin(pos / 1f) + 1.2f), c.z);
        }
        else if (state == ProductionState.starting)
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
