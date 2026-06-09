using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Water : MonoBehaviour
{
    float[] waterLevel;
    float[] waterLevelStep;
    float[] waterLevelPrevStep;

    float[] flowX;
    float[] flowY;

    public TextAsset terrainData;
    byte[] terrainDataBytes;

    const int WATER_SIZE_X = 200, WATER_SIZE_Y = 200;
    const int WATER_OFFSET_X = 130, WATER_OFFSET_Y = 130;

    float lastUpdate;

    // Start is called before the first frame update
    void Start()
    {
        var mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        terrainDataBytes = terrainData.bytes;

        waterLevel = new float[WATER_SIZE_X * WATER_SIZE_Y];
        flowX = new float[WATER_SIZE_X * WATER_SIZE_Y];
        flowY = new float[WATER_SIZE_X * WATER_SIZE_Y];
        waterLevelStep = new float[WATER_SIZE_X * WATER_SIZE_Y];
        waterLevelPrevStep = new float[WATER_SIZE_X * WATER_SIZE_Y];
        
        for (int y = 0; y < WATER_SIZE_Y; y++)
            for (int x = 0; x < WATER_SIZE_X; x++)
            {
                waterLevel[x + y * WATER_SIZE_X] = 0; // -1;
                waterLevelStep[x + y * WATER_SIZE_X] = 0; // -1;
                waterLevelPrevStep[x + y * WATER_SIZE_X] = 0; // -1;
            }
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
                swl(x + WATER_OFFSET_X, y + WATER_OFFSET_Y, 1.5f);
            swl(x + WATER_OFFSET_X, (WATER_OFFSET_Y - 1), 0);
            swl(x + WATER_OFFSET_X, WATER_OFFSET_Y + 10, 0);
        }
        for (int y = 0; y < 10; y++)
        {
            swl(WATER_OFFSET_X - 1, y + WATER_OFFSET_Y, 0);
            swl(WATER_OFFSET_X + 10, y + WATER_OFFSET_Y, 0);
        }
        
        for (int y = 0; y < WATER_SIZE_Y; y++)
            for (int x = 0; x < WATER_SIZE_X; x++)
                waterLevelPrevStep[x + y * WATER_SIZE_X] = waterLevel[x + y * WATER_SIZE_X];
        lastUpdate = 0.0f;
    }

    public float terrainHeight(int x, int y)
    {
        // XXX somehow the terrain data has flipped axis, let's not care for now
        return ((float)terrainDataBytes[y + x * Terrain.TERRAIN_SIZE]) / 255 * Terrain.HEIGHT_SCALE * Terrain.SCALE;
    }

    float wl(int x, int y)
    {
        return waterLevel[x + y * WATER_SIZE_X];
    }

    float wlT(int x, int y)
    {
        var c = wl(x, y);
        //if (c == -1)
        //    c = 0;
        return c + terrainHeight(x, y);
    }

    void swl(int x, int y, float val)
    {
        waterLevel[x + y * WATER_SIZE_X] = val;
    }

    void moveWater()
    {
        const float FC = 0.003f;

        float flowx(int x, int y)
        {
            return flowX[x + y * WATER_SIZE_X];
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

        for (int y = 1; y < WATER_SIZE_Y - 1; y++)
            for (int x = 1; x < WATER_SIZE_X - 1; x++)
            {
                flowX[x + y * WATER_SIZE_X] += FC * dt * (wlT(x - 1, y) - wlT(x, y));
                flowY[x + y * WATER_SIZE_X] += FC * dt * (wlT(x, y - 1) - wlT(x, y));
            }

        for (int y = 1; y < WATER_SIZE_Y - 1; y++)
            for (int x = 1; x < WATER_SIZE_X - 1; x++)
            {
                float total = zeroclip(-flowx(x, y)) + zeroclip(-flowy(x, y)) + zeroclip(flowx(x + 1, y)) + zeroclip(flowy(x, y + 1));
                float max_outflow = wl(x, y) / dt;
                if (total > 0)
                {
                    var scale = Mathf.Min(1f, max_outflow / total);
                    if (flowx(x, y) < 0)
                        flowX[x + y * WATER_SIZE_X] *= scale;
                    if (flowy(x, y) < 0)
                        flowY[x + y * WATER_SIZE_X] *= scale;
                    if (flowx(x + 1, y) > 0)
                        flowX[x + 1 + y * WATER_SIZE_X] *= scale;
                    if (flowy(x, y + 1) > 0)
                        flowY[x + (y + 1) * WATER_SIZE_X] *= scale;

                }
            }

        for (int y = 1; y < WATER_SIZE_Y - 1; y++)
            for (int x = 1; x < WATER_SIZE_X - 1; x++)
            {
                waterLevel[x + y * WATER_SIZE_X] += (flowx(x, y) + flowy(x, y) - flowx(x + 1, y) - flowy(x, y + 1)) / dt;
            }


        return;
    }

    void updateWaterTexture()
    {
        int start = 1, end = 199;
        int size = end - start;
        var vertices = new Vector3[size * size];
        var tris = new List<int>();

        int c = 0;
        for (int y = start; y < end; y++)
            for (int x = start; x < end; x++)
            {
                vertices[c] = new Vector3(x * Terrain.SCALE, wl(x, y) + terrainHeight(x, y) - 0.001f, y * Terrain.SCALE);
                if (y < end - 1 && x < end - 1)
                    //&& (visible(x, y) || visible(x, y + 1) || visible(x + 1, y) || visible(x + 1, y + 1)))
                {
                    int ix = x - start;
                    int iy = y - start;
                    tris.Add(ix + iy * size);
                    tris.Add(ix + (iy + 1) * size);
                    tris.Add((ix + 1) + (iy + 1) * size);
                    tris.Add(ix + iy * size);
                    tris.Add((ix + 1) + (iy + 1) * size);
                    tris.Add(ix + 1 + iy * size);
                }
                c++;
            }

        var mesh = GetComponent<MeshFilter>().mesh;
        mesh.vertices = vertices;
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
