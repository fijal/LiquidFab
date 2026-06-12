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
    NativeArray<float> flowX;
    NativeArray<float> flowY;
    int sizeX, sizeY;
    NativeArray<float> source;
    public float viscosity;
    public float maxAngle;
    public float friction;
    public float mass;
    const float gravity = 0.3f;

    public float BOUNDARY_FLOW;

    SimulationType simulationType;
    NativeArray<float> terrain;

    public Simulation(Terrain terrain, SimulationType tp, NativeArray<float> source, int sizeX, int sizeY)
    {
        flowX = new NativeArray<float>((sizeX + 1) * sizeY, Allocator.Persistent);
        flowY = new NativeArray<float>(sizeX * (sizeY + 1), Allocator.Persistent);
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.source = source;
        this.simulationType = tp;
        this.terrain = terrain.terrainHeight;

        viscosity = 0;
        maxAngle = 0;
        friction = 0.05f; // 0 - 1
        BOUNDARY_FLOW = 0;
        mass = 1;
    }

    public void Dispose()
    {
        if (flowX != null)
            flowX.Dispose();
        if (flowY != null)
            flowY.Dispose();
    }

    /*float flowx(int x, int y)
    {
        return flowX[x + (y * (sizeX + 1))];
    }

    float flowy(int x, int y)
    {
        return flowY[x + y * sizeX];
    }*/

    /*float readAtPos(int x, int y)
    {
        //if (simulationType == SimulationType.Water)
        //{
        return source[x + y * sizeX] + terrain.terrainHeight[x + y * sizeX];
        //} else if (simulationType == SimulationType.Terrain)
        //{
        //    return terrain.height(x, y);
        //} else
        //{
        //    return 0;
        //}
    }*/


    public void Execute()
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
