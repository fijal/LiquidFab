using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FenceBehaviour : BuildingGridPlacement
{
    const int MAX_CHAIN_LENGTH = 15;

    public FenceBehaviour(BuildingSpec spec) : base(spec) {}

    public override int GetMaxChainLength()
    {
        return MAX_CHAIN_LENGTH;
    }

    public override void spawnObject(Terrain terrain, Vector3 pos, Quaternion rot)
    {
        terrain.spawnBuilding(spec.prefab, pos, rot);
    }
}

public class Fence : MonoBehaviour
{
}
