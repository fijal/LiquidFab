using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public enum SimulationType
{
    Water = 1,
    Terrain = 2,
    SubWater = 3
}

public struct Simulation : IJob
{
    public NativeArray<float> waterFlowX;
    public NativeArray<float> waterFlowY;
    NativeArray<float> terrainFlowX;
    NativeArray<float> terrainFlowY;
    int sizeX, sizeY;

    public NativeArray<float> subFlowX, subFlowY;
    public NativeArray<float> subLevel;

    public NativeArray<float> water;
    public NativeArray<float> walls; // where we force the flow to be zero
    
    public float viscosity;
    public float maxAngle;
    public float friction;
    public float mass;
    const float gravity = 0.15f;

    public float BOUNDARY_FLOW;

    const float SUB_TERRAIN_FAC = 0.1f; // how much the height of ground affects subterranean flow
    const float SUB_SATURATION = 0.5f; // don't seep if ground is saturated

    public NativeArray<float> terrain;

    public Simulation(int sizeX, int sizeY)
    {
        waterFlowX = new NativeArray<float>((sizeX + 1) * sizeY, Allocator.Persistent);
        waterFlowY = new NativeArray<float>(sizeX * (sizeY + 1), Allocator.Persistent);
        terrainFlowX = new NativeArray<float>((sizeX + 1) * sizeY, Allocator.Persistent);
        terrainFlowY = new NativeArray<float>(sizeX * (sizeY + 1), Allocator.Persistent);
        subFlowX = new NativeArray<float>((sizeX + 1) * sizeY, Allocator.Persistent);
        subFlowY = new NativeArray<float>(sizeX * (sizeY + 1), Allocator.Persistent);

        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.water = new NativeArray<float>(sizeX * sizeY, Allocator.Persistent);
        this.terrain = new NativeArray<float>(sizeX * sizeY, Allocator.Persistent);
        subLevel = new NativeArray<float>(sizeX * sizeY, Allocator.Persistent);
        walls = new NativeArray<float>(sizeX * sizeY, Allocator.Persistent);

        viscosity = 0;
        maxAngle = 0;
        friction = 0.05f; // 0 - 1
        BOUNDARY_FLOW = 0;
        mass = 1;
    }

    public void Dispose()
    {
        if (waterFlowX != null)
            waterFlowX.Dispose();
        if (waterFlowY != null)
            waterFlowY.Dispose();
        if (terrainFlowX != null)
            terrainFlowX.Dispose();
        if (terrainFlowY != null)
            terrainFlowY.Dispose();
        if (water != null)
            water.Dispose();

        if (terrain != null)
            terrain.Dispose();
        if (subFlowX != null)
            subFlowX.Dispose();
        if (subFlowY != null)
            subFlowY.Dispose();
        if (subLevel != null)
            subLevel.Dispose();
        if (walls != null)
            walls.Dispose();
    }

    public void Execute()
    {
        // modify terrain first
        //friction = 0.5f;
        //BOUNDARY_FLOW = 0;
        //s.viscosity = 0.1f;
        //maxAngle = 0.1f;
        //mass = 1f;
        //viscosity = 0;
        //subExecute(SimulationType.Terrain, this.terrain, terrainFlowX, terrainFlowY);
        // then modify water
        friction = 0.05f;
        viscosity = 0.0f;
        BOUNDARY_FLOW = 0; //  -1;
        maxAngle = 0f;
        subExecute(SimulationType.Water, this.water, waterFlowX, waterFlowY);
        friction = 0.5f;
        viscosity = 2f;
        subExecute(SimulationType.SubWater, subLevel, subFlowX, subFlowY);
    }

    void subExecute(SimulationType simulationType, NativeArray<float> source, NativeArray<float> flowX, NativeArray<float> flowY)
    {
        const float dt = 0.5f;
        var frictionFactor = Mathf.Pow(1 - friction, dt);

        for (int i = 0; i < sizeX; ++i)
        {
            flowX[i * (sizeX + 1)] = BOUNDARY_FLOW;
            flowX[sizeX + i * (sizeX + 1)] = -BOUNDARY_FLOW;
            flowY[i] = BOUNDARY_FLOW;
            flowY[i + sizeX * (sizeX - 1)] = -BOUNDARY_FLOW;
        }

        if (maxAngle > 0)
        {
            for (int y = 1; y < sizeY; y++)
                for (int x = 1; x < sizeX + 1; x++)
                    flowX[x + y * (sizeX + 1)] = 0;
            for (int y = 1; y < sizeY + 1; y++)
                for (int x = 1; x < sizeX; x++)
                    flowY[x + y * sizeX] = 0;
        }

        for (int y = 0; y < sizeY; y++)
            for (int x = 1; x < sizeX; x++)
            {
                if (walls[x + y * sizeX] > 0)
                {
                    flowX[x + y * (sizeX + 1)] = 0;
                    continue;
                }
                float v;
                if (simulationType == SimulationType.Water)
                    v = (source[x - 1 + y * sizeX] + terrain[x - 1 + y * sizeX]) - (source[x + y * sizeX] + terrain[x + y * sizeX]);
                else if (simulationType == SimulationType.SubWater)
                    v = (source[x - 1 + y * sizeX] + terrain[x - 1 + y * sizeX] * SUB_TERRAIN_FAC)
                        - (source[x + y * sizeX] + terrain[x + y * sizeX] * SUB_TERRAIN_FAC);
                else
                    v = source[x - 1 + y * sizeX] - source[x + y * sizeX];
                CheckFinite(v);
                v *= mass * gravity * dt;
                if (maxAngle > 0)
                {
                    if (v > 0 && v < maxAngle)
                        v = 0;
                    if (v > 0)
                        v -= maxAngle;
                    if (v < 0 && v > -maxAngle)
                        v = 0;
                    if (v < 0)
                        v += maxAngle;
                }
                flowX[x + y * (sizeX + 1)] = flowX[x + y * (sizeX + 1)] * frictionFactor + v;
                CheckFinite(flowX[x + y * (sizeX + 1)]);
            }

        for (int y = 1; y < sizeY; y++)
            for (int x = 0; x < sizeX; x++)
            {
                if (walls[x + y * sizeX] > 0)
                {
                    flowY[x + y * sizeX] = 0;
                    continue;
                }
                float v;
                if (simulationType == SimulationType.Water)
                    v = (source[x + (y - 1) * sizeX] + terrain[x + (y - 1) * sizeX]) - (source[x + y * sizeX] + terrain[x + y * sizeX]);
                else if (simulationType == SimulationType.SubWater)
                    v = (source[x + (y - 1) * sizeX] + terrain[x + (y - 1) * sizeX] * SUB_TERRAIN_FAC) -
                        (source[x + y * sizeX] + terrain[x + y * sizeX] * SUB_TERRAIN_FAC);
                else
                    v = (source[x + (y - 1) * sizeX]) - (source[x + y * sizeX]);
                CheckFinite(v);
                v *= frictionFactor * mass * gravity * dt;
                if (maxAngle > 0)
                {
                    if (v > 0 && v < maxAngle)
                        v = 0;
                    if (v > 0)
                        v -= maxAngle;
                    if (v < 0 && v > -maxAngle)
                        v = 0;
                    if (v < 0)
                        v += maxAngle;
                }
                flowY[x + y * sizeX] = flowY[x + y * sizeX] * frictionFactor + v;
                CheckFinite(flowY[x + y * sizeX]);
            }

        // viscosity
        if (viscosity > 0)
        {
            Debug.Assert(simulationType == SimulationType.Water || simulationType == SimulationType.SubWater);
            for (int y = 0; y < sizeY; ++y)
                for (int x = 1; x < sizeX; ++x)
                {
                    float H;
                    if (flowX[x + (y * (sizeX + 1))] > 0f)
                        H = source[x - 1 + y * sizeX] + terrain[x - 1 + y * sizeX];
                    else
                        H = source[x + y * sizeX] + terrain[x + y * sizeX];
                    H *= H;

                    if (H > 0f)
                    {
                        flowX[x + y * (sizeX + 1)] *= H / (H + 3 * dt * viscosity);
                        CheckFinite(flowX[x + y * (sizeX + 1)]);
                    }
                }
            for (int y = 1; y < sizeY; ++y)
                for (int x = 0; x < sizeX; ++x)
                {
                    float H;
                    if (flowY[x + (y * sizeX)] > 0f)
                        H = source[x + (y - 1) * sizeX] + terrain[x + (y - 1) * sizeX];
                    else
                        H = source[x + y * sizeX] + terrain[x + y * sizeX];
                    H *= H;

                    if (H > 0f)
                    {
                        flowY[x + y * sizeX] *= H / (H + 3 * dt * viscosity);
                        CheckFinite(flowY[x + y * sizeX]);
                    }
                }
        }

        for (int y = 0; y < sizeY; y++)
            for (int x = 0; x < sizeX; x++)
            {
                float total = Mathf.Max(0, -flowX[x + y * (sizeX + 1)]) + Mathf.Max(0, -flowY[x + y * sizeX]) +
                    Mathf.Max(0, flowX[x + 1 + y * (sizeX + 1)]) + Mathf.Max(0, flowY[x + (y + 1) * sizeX]);
                float max_outflow = source[x + y * sizeX] / dt;
                if (total > 0)
                {
                    var scale = max_outflow / total;

                    /* clamp 'scale' between 0 and 1.  NaNs are turned to 1. */
                    if (!(scale <= 1f))
                        scale = 1f;
                    if (!(scale >= 0f))
                        scale = 0;

                    if (flowX[x + y * (sizeX + 1)] < 0)
                        flowX[x + y * (sizeX + 1)] *= scale;
                    if (flowY[x + y * sizeX] < 0)
                        flowY[x + y * sizeX] *= scale;
                    if (flowX[x + 1 + y * (sizeX + 1)] > 0)
                        flowX[x + 1 + y * (sizeX + 1)] *= scale;
                    if (flowY[x + (y + 1) * sizeX] > 0)
                        flowY[x + (y + 1) * sizeX] *= scale;
                }
            }

        for (int y = 0; y < sizeY; y++)
            for (int x = 0; x < sizeX; x++)
            {
                var seepage = 0f;
                if (simulationType == SimulationType.Water)
                    seepage = 0.0005f + 0.0002f * water[x + y * sizeX];
                if (simulationType == SimulationType.SubWater)
                    seepage = 0.00005f;

                // XXX <tmp>
                //seepage = 0;
                // XXX </tmp>

                CheckFinite(seepage);
                var cur = (flowX[x + y * (sizeX + 1)] + flowY[x + y * sizeX] - flowX[x + 1 + y * (sizeX + 1)]
                    - flowY[x + (y + 1) * sizeX]);
                CheckFinite(cur);
                if (seepage > source[x + y * sizeX])
                    seepage = source[x + y * sizeX];
                if (simulationType == SimulationType.Water)
                    if (subLevel[x + y * sizeX] + seepage > SUB_SATURATION)
                        seepage = SUB_SATURATION - subLevel[x + y * sizeX];
                CheckFinite(seepage);
                source[x + y * sizeX] += (cur - seepage) * dt;
                CheckFinite(source[x + y * sizeX]);
                if (simulationType == SimulationType.Water)
                {
                    subLevel[x + y * sizeX] += seepage * dt;
                    CheckFinite(subLevel[x + y * sizeX]);
                }
            }


        return;
    }

    static void CheckFinite(float a)
    {
        if (!float.IsFinite(a))
        {
            Debug.LogAssertion("non-finite number!  put a breakpoint here in Simulation.cs");
        }
    }
}
