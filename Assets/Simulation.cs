using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISimulation
{
    public float readAtPos(int x, int y);
    public float[] getData();
}

public class Simulation
{
    float[] flowX;
    float[] flowY;
    int sizeX, sizeY;
    ISimulation source;
    float viscosity;

    public Simulation(int sizeX, int sizeY, int tiles, ISimulation source, float viscosity=0)
    {
        flowX = new float[(sizeX + 1) * sizeY];
        flowY = new float[sizeX * (sizeY + 1)];
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.source = source;
        this.viscosity = viscosity;
    }

    float flowXAt(int x, int y)
    {
        return flowX[x + y * (sizeX + 1)];
    }

    float flowYAt(int x, int y)
    {
        return flowY[x + y * sizeX];
    }

    public void Step()
    {
        const float FC = 0.003f;
        float[] data = source.getData();

        float flowx(int x, int y)
        {
            return flowX[x + (y * (sizeX + 1))];
        }

        float flowy(int x, int y)
        {
            return flowY[x + y * sizeX];
        }

        const float dt = 1f;
        const float BOUNDARY_FLOW = 0f;

        for (int i = 0; i < sizeX; ++i)
        {
            flowX[i * (sizeX + 1)] = BOUNDARY_FLOW;
            flowX[sizeX + i * (sizeX + 1)] = BOUNDARY_FLOW;
            flowY[i] = BOUNDARY_FLOW;
            flowY[i + sizeX * (sizeX - 1)] = BOUNDARY_FLOW;
        }

        for (int y = 0; y < sizeY; y++)
            for (int x = 1; x < sizeX; x++)
                flowX[x + y * (sizeX + 1)] += FC * dt * (source.readAtPos(x - 1, y) - source.readAtPos(x, y));
        for (int y = 1; y < sizeY; y++)
            for (int x = 0; x < sizeX; x++)
                flowY[x + y * sizeX] += FC * dt * (source.readAtPos(x, y - 1) - source.readAtPos(x, y));

        // viscosity
        if (viscosity > 0)
        {
            for (int y = 0; y < sizeY; ++y)
            {
                for (int x = 1; x < sizeX; ++x)
                {
                    float H = (flowXAt(x, y) > 0f) ? source.readAtPos(x - 1, y) : source.readAtPos(x, y);
                    H *= H;

                    if (H > 0f)
                        flowX[x + y * (sizeX + 1)] *= H / (H + 3 * dt * viscosity);
                }
            }
            for (int y = 1; y < sizeY; ++y)
            {
                for (int x = 0; x < sizeX; ++x)
                {
                    float H = (flowYAt(x, y) > 0f) ? source.readAtPos(x, y - 1) : source.readAtPos(x, y);
                    H *= H;

                    if (H > 0f)
                        flowY[x + y * sizeX] *= H / (H + 3 * dt * viscosity);
                }
            }
        }

        for (int y = 0; y < sizeY; y++)
            for (int x = 0; x < sizeX; x++)
            {
                float total = Mathf.Max(0, -flowx(x, y)) + Mathf.Max(0, -flowy(x, y)) + Mathf.Max(0, flowx(x + 1, y)) + Mathf.Max(0, flowy(x, y + 1));
                float max_outflow = data[x + y * sizeX] / dt;
                if (total > 0)
                {
                    var scale = Mathf.Min(1f, max_outflow / total);
                    if (flowx(x, y) < 0)
                        flowX[x + y * (sizeX + 1)] *= scale;
                    if (flowy(x, y) < 0)
                        flowY[x + y * sizeX] *= scale;
                    if (flowx(x + 1, y) > 0)
                        flowX[x + 1 + y * (sizeX + 1)] *= scale;
                    if (flowy(x, y + 1) > 0)
                        flowY[x + (y + 1) * sizeX] *= scale;

                }
            }

        for (int y = 0; y < sizeY; y++)
            for (int x = 0; x < sizeX; x++)
            {
                data[x + y * sizeX] += (flowx(x, y) + flowy(x, y) - flowx(x + 1, y) - flowy(x, y + 1)) / dt;
            }


        return;
    }
}

