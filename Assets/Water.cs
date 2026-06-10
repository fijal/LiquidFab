using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Water : MonoBehaviour
{
    float[] waterLevel;
    
    float[] flowX;
    float[] flowY;

    const int WATER_SIZE_X = 200, WATER_SIZE_Y = 200;
    const int WATER_OFFSET_X = 130, WATER_OFFSET_Y = 130;

    float lastUpdate;

    // Start is called before the first frame update
    void Start()
    {
        var mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        
        waterLevel = new float[WATER_SIZE_X * WATER_SIZE_Y];
        flowX = new float[(WATER_SIZE_X + 1) * WATER_SIZE_Y];
        flowY = new float[WATER_SIZE_X * (WATER_SIZE_Y + 1)];
        
        for (int y = 0; y < WATER_SIZE_Y; y++)
            for (int x = 0; x < WATER_SIZE_X; x++)
            {
                waterLevel[x + y * WATER_SIZE_X] = 0;
            }
        
        lastUpdate = 0.0f;
    }

    float wl(int x, int y)
    {
        return waterLevel[x + y * WATER_SIZE_X];
    }

    float wlT(int x, int y)
    {
        return wl(x, y) + transform.parent.GetComponent<Terrain>().height(x, y);
    }

    void swl(int x, int y, float val)
    {
        waterLevel[x + y * WATER_SIZE_X] = val;
    }

    void moveWater()
    {
        const float FC = 0.003f;

        swl(130, 130, wl(130, 130) + 0.25f);
        swl(130, 131, wl(130, 131) + 0.25f);
        swl(131, 130, wl(131, 130) + 0.25f);
        swl(131, 131, wl(131, 131) + 0.25f);


        float flowx(int x, int y)
        {
            return flowX[x + (y * (WATER_SIZE_X + 1))];
        }

        float flowy(int x, int y)
        {
            return flowY[x + y * WATER_SIZE_X];
        }

        float zeroclip(float f)
        {
            return f < 0 ? 0 : f;
        }

        const float dt = 1f;
        const float BOUNDARY_FLOW = 0f;

        for (int i = 0; i < WATER_SIZE_X; ++i)
        {
            flowX[i * (WATER_SIZE_X + 1)] = BOUNDARY_FLOW;
            flowX[WATER_SIZE_X + i * (WATER_SIZE_X + 1)] = BOUNDARY_FLOW;
            flowY[i] = BOUNDARY_FLOW;
            flowY[i + WATER_SIZE_X * (WATER_SIZE_X - 1)] = BOUNDARY_FLOW;
        }

        for (int y = 0; y < WATER_SIZE_Y; y++)
            for (int x = 1; x < WATER_SIZE_X; x++)
                flowX[x + y * (WATER_SIZE_X + 1)] += FC * dt * (wlT(x - 1, y) - wlT(x, y));
        for (int y = 1; y < WATER_SIZE_Y; y++)
            for (int x = 0; x < WATER_SIZE_X; x++)
                flowY[x + y * WATER_SIZE_X] += FC * dt * (wlT(x, y - 1) - wlT(x, y));

        for (int y = 0; y < WATER_SIZE_Y; y++)
            for (int x = 0; x < WATER_SIZE_X; x++)
            {
                float total = zeroclip(-flowx(x, y)) + zeroclip(-flowy(x, y)) + zeroclip(flowx(x + 1, y)) + zeroclip(flowy(x, y + 1));
                float max_outflow = wl(x, y) / dt;
                if (total > 0)
                {
                    var scale = Mathf.Min(1f, max_outflow / total);
                    if (flowx(x, y) < 0)
                        flowX[x + y * (WATER_SIZE_X + 1)] *= scale;
                    if (flowy(x, y) < 0)
                        flowY[x + y * WATER_SIZE_X] *= scale;
                    if (flowx(x + 1, y) > 0)
                        flowX[x + 1 + y * (WATER_SIZE_X + 1)] *= scale;
                    if (flowy(x, y + 1) > 0)
                        flowY[x + (y + 1) * WATER_SIZE_X] *= scale;

                }
            }

        for (int y = 0; y < WATER_SIZE_Y; y++)
            for (int x = 0; x < WATER_SIZE_X; x++)
            {
                waterLevel[x + y * WATER_SIZE_X] += (flowx(x, y) + flowy(x, y) - flowx(x + 1, y) - flowy(x, y + 1)) / dt;
            }


        return;
    }

    void updateWaterTexture()
    {
        int start = 0, end = 200;
        int size = end - start;
        var vertices = new Vector3[size * size];
        var uvs = new Vector2[size * size];
        var tris = new List<int>();

        int c = 0;
        for (int y = start; y < end; y++)
            for (int x = start; x < end; x++)
            {
                vertices[c] = new Vector3(x * Terrain.SCALE, wl(x, y) + transform.parent.GetComponent<Terrain>().height(x, y) - 0.001f, y * Terrain.SCALE);
                uvs[c] = new Vector2(0, wl(x, y));
                if (y < end - 1 && x < end - 1)
                {
                    int ix = x - start;
                    int iy = y - start;
                    tris.Add(ix + iy * size);
                    tris.Add(ix + (iy + 1) * size);
                    tris.Add(ix + 1 + iy * size);
                    tris.Add(ix + (iy + 1) * size);
                    tris.Add((ix + 1) + (iy + 1) * size);
                    tris.Add(ix + 1 + iy * size);
                }
                c++;
            }

        var mesh = GetComponent<MeshFilter>().mesh;
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return;
    }

    // Update is called once per frame
    void Update()
    {
        if (lastUpdate <= 0)
        {
            moveWater();
            updateWaterTexture();
            lastUpdate = 0.1f;
        } else
        {
            lastUpdate -= Time.deltaTime;
        }
    }
}
