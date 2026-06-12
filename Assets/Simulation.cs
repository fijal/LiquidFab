using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public enum SimulationType
{
    Water = 1,
    Terrain = 2
}

public struct Simulation : IJob
{
    NativeArray<float> waterFlowX;
    NativeArray<float> waterFlowY;
    NativeArray<float> terrainFlowX;
    NativeArray<float> terrainFlowY;
    int sizeX, sizeY;
    NativeArray<float> water;
    public float viscosity;
    public float maxAngle;
    public float friction;
    public float mass;
    const float gravity = 0.3f;

    public float BOUNDARY_FLOW;

    public NativeArray<float> terrain;

    public Simulation(NativeArray<float> water, int sizeX, int sizeY)
    {
        waterFlowX = new NativeArray<float>((sizeX + 1) * sizeY, Allocator.Persistent);
        waterFlowY = new NativeArray<float>(sizeX * (sizeY + 1), Allocator.Persistent);
        terrainFlowX = new NativeArray<float>((sizeX + 1) * sizeY, Allocator.Persistent);
        terrainFlowY = new NativeArray<float>(sizeX * (sizeY + 1), Allocator.Persistent);

        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.water = water;
        this.terrain = new NativeArray<float>(sizeX * sizeY, Allocator.Persistent);

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

        if (terrain != null)
            terrain.Dispose();
    }

    public readonly NativeArray<float> GetFlowX() => waterFlowX;
    public readonly NativeArray<float> GetFlowY() => waterFlowY;

    public void Execute()
    {
        // modify terrain first
        friction = 0.5f;
        BOUNDARY_FLOW = 0;
        //s.viscosity = 0.1f;
        maxAngle = 0.1f;
        mass = 1f;
        subExecute(SimulationType.Terrain, this.terrain, terrainFlowX, terrainFlowY);
        // then modify water
        friction = 0.05f;
        viscosity = 0.1f;
        BOUNDARY_FLOW = -1;
        maxAngle = 0f;
        subExecute(SimulationType.Water, this.water, waterFlowX, waterFlowY);
    }

    void subExecute(SimulationType simulationType, NativeArray<float> source, NativeArray<float> flowX, NativeArray<float> flowY)
    {
        const float dt = 1f;
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
                float v;
                if (simulationType == SimulationType.Water)
                    v = (source[x - 1 + y * sizeX] + terrain[x - 1 + y * sizeX]) - (source[x + y * sizeX] + terrain[x + y * sizeX]);
                else
                    v = source[x - 1 + y * sizeX] - source[x + y * sizeX];
                // v = (readAtPos(x - 1, y) - readAtPos(x, y));
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
                flowX[x + y * (sizeX + 1)] += v;
            }

        for (int y = 1; y < sizeY; y++)
            for (int x = 0; x < sizeX; x++)
            {
                float v;
                if (simulationType == SimulationType.Water)
                    v = (source[x + (y - 1) * sizeX] + terrain[x + (y - 1) * sizeX]) - (source[x + y * sizeX] + terrain[x + y * sizeX]);
                else
                    v = (source[x + (y - 1) * sizeX]) - (source[x + y * sizeX]);
                //v = (readAtPos(x, y - 1) - readAtPos(x, y));
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
                flowY[x + y * sizeX] += v;
            }

        // viscosity
        if (viscosity > 0)
        {
            Debug.Assert(simulationType == SimulationType.Water);
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
                        flowX[x + y * (sizeX + 1)] *= H / (H + 3 * dt * viscosity);
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
                        flowY[x + y * sizeX] *= H / (H + 3 * dt * viscosity);
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
                    var scale = Mathf.Min(1f, max_outflow / total);
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
                source[x + y * sizeX] += (flowX[x + y * (sizeX + 1)] + flowY[x + y * sizeX] - flowX[x + 1 + y * (sizeX + 1)]
                    - flowY[x + (y + 1) * sizeX]) / dt;
            }


        return;
    }
}
