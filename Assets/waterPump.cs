using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterPumpTool : BuildingFreePlacement
{
    public WaterPumpTool(BuildingSpec spec) : base(spec)
    {
    }

    public override void clickTerrain(GameObject highlight, Terrain terrain, Vector3 hitPoint)
    {
        if (isLegalPlacement(highlight, terrain, hitPoint))
            terrain.spawnWaterPump(spec.prefab, hitPoint, highlight.transform.rotation);
    }

    public override bool isLegalPlacement(GameObject highlight, Terrain terrain, Vector3 point)
    {
        return true;
    }

}


public class waterPump : MonoBehaviour
{
    float pos = 0;
    public float basePos = 0;

    public float fuelLevel;
    public int logs = 0;
    public ParticleSystem smoke;
    public const float MAX_FUEL = 180;

    void FixedUpdate()
    {
        if (fuelLevel <= 0)
        {
            return;
        }
        fuelLevel -= Time.fixedDeltaTime;
        if (fuelLevel <= 0)
        {
            if (logs > 0)
            {
                logs--;
                fuelLevel = MAX_FUEL;
            }
            else
            {
                fuelLevel = 0;
                smoke.Stop();
                return;
            }
        }

        pos += Time.fixedDeltaTime;
        var c = transform.position;
        transform.position = new Vector3(c.x, basePos + 0.05f * (Mathf.Sin(pos * 3f) - 1), c.z);
    }

    public void interact(Controls controls)
    {
        var g = Instantiate(controls.detailsPanel);
        g.GetComponent<DetailsPanel>().controls = controls;
        g.GetComponent<DetailsPanel>().interactable = gameObject;
    }

    public bool maybeConsumeLog()
    {
        if (logs >= 1 && fuelLevel == 0)
        {
            smoke.Play();
            fuelLevel = MAX_FUEL;
            logs--;
        } else if (fuelLevel > 0)
        {
            smoke.Play();
        }
        return true;
    }

    public bool feedFuel()
    {
        logs++;
        return maybeConsumeLog();
    }
}
