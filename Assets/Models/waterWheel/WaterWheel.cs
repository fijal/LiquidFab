using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterWheelBehaviour : BuildingGridPlacement
{
    WaterWheelSpec wheelSpec;
    const int MAX_CHAIN_LENGTH = 6;

    public override int GetMaxChainLength()
    {
        return MAX_CHAIN_LENGTH;
    }

    public WaterWheelBehaviour(BuildingSpec spec, WaterWheelSpec wheelSpec) : base(spec)
    {
        this.wheelSpec = wheelSpec;
    }

    public override void spawnObject(Terrain terrain, Vector3 pos, Quaternion rot)
    {
        terrain.spawnWaterWheel(pos, rot);
    }
}

public class WaterWheel : MonoBehaviour
{
    public GameObject gear1, gear2, gear3;
    
    // Update is called once per frame
    void Update()
    {
        gear1.transform.localRotation *= Quaternion.Euler(0, 0, 180 * Time.deltaTime);
        gear2.transform.localRotation *= Quaternion.Euler(0, 0, 180 * Time.deltaTime);
        gear3.transform.localRotation *= Quaternion.Euler(0, 0, 180 * Time.deltaTime);
    }
}
