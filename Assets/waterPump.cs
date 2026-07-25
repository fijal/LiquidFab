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
}
