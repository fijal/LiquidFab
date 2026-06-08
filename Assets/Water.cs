using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Water : MonoBehaviour
{
    float[] waterLevel;
    float[] waterLevelStep;
    float[] waterLevelPrevStep;
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
        waterLevelStep = new float[WATER_SIZE_X * WATER_SIZE_Y];
        waterLevelPrevStep = new float[WATER_SIZE_X * WATER_SIZE_Y];
        
        for (int y = 0; y < WATER_SIZE_Y; y++)
            for (int x = 0; x < WATER_SIZE_X; x++)
            {
                waterLevel[x + y * WATER_SIZE_X] = -1;
                waterLevelStep[x + y * WATER_SIZE_X] = -1;
                waterLevelPrevStep[x + y * WATER_SIZE_X] = -1;
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
        if (c == -1)
            c = 0;
        return c + terrainHeight(x, y);
    }

    void swl(int x, int y, float val)
    {
        waterLevel[x + y * WATER_SIZE_X] = val;
    }

    int calculateVertexCount(int[] offsets, int[] relOffsets)
    {
        for (int y = 0; y < WATER_SIZE_Y; y++)
            offsets[y] = -1;

        int vert = 0;
        for (int y = 0; y < WATER_SIZE_Y; ++y)
        {
            for (int x = 0; x < WATER_SIZE_X; ++x)
            {
                if (waterLevel[x + y * WATER_SIZE_X] >= 0)
                {
                    if (offsets[y] == -1)
                    {
                        relOffsets[y] = x;
                        offsets[y] = vert;
                    }
                    vert++;
                }
            }
        }
        return vert;
    }

    void populateVertices(Vector3[] vert)
    {
        int c = 0;
        for (int y = 0; y < WATER_SIZE_Y; ++y)
            for (int x = 0; x < WATER_SIZE_X; ++x)
                if (wl(x, y) >= 0)
                {
                    vert[c] = new Vector3(x * Terrain.SCALE, wl(x, y) + terrainHeight(x, y), y * Terrain.SCALE);
                    c++;
                }
    }

    void populateTriangles(int[] triangles, int[] offsets, int[] relOffsets)
    {
        var t = 0;
        for (int y = 1; y < WATER_SIZE_Y - 1; ++y)
        {
            for (int x = 1; x < WATER_SIZE_X - 1; ++x)
            {
                if (wl(x, y) >= 0 && wl(x + 1, y) >= 0 && wl(x, y + 1) >= 0 && wl(x + 1, y + 1) >= 0)
                {
                    triangles[t + 2] = offsets[y] + x - relOffsets[y];
                    triangles[t + 1] = offsets[y] + x + 1 - relOffsets[y];
                    triangles[t + 0] = offsets[y + 1] + x - relOffsets[y + 1];
                    triangles[t + 3] = offsets[y] + x + 1 - relOffsets[y];
                    triangles[t + 4] = offsets[y + 1] + x - relOffsets[y + 1];
                    triangles[t + 5] = offsets[y + 1] + x + 1 - relOffsets[y + 1];
                    t += 6;
                    continue;
                }
                if (wl(x, y) >= 0 && wl(x + 1, y) >= 0 && wl(x, y + 1) >= 0)
                {
                    triangles[t + 2] = offsets[y] + x - relOffsets[y];
                    triangles[t + 1] = offsets[y] + x + 1 - relOffsets[y];
                    triangles[t + 0] = offsets[y + 1] + x - relOffsets[y + 1];
                    t += 3;
                    continue;
                }
                if (wl(x + 1, y) >= 0 && wl(x + 1, y + 1) >= 0 && wl(x, y + 1) >= 0)
                {
                    triangles[t + 2] = offsets[y] + x + 1 - relOffsets[y];
                    triangles[t + 1] = offsets[y + 1] + x + 1 - relOffsets[y + 1];
                    triangles[t + 0] = offsets[y + 1] + x - relOffsets[y + 1];
                    t += 3;
                    continue;
                }
                if (wl(x, y) >= 0 && wl(x + 1, y) >= 0 && wl(x + 1, y + 1) >= 0)
                {
                    triangles[t + 2] = offsets[y] + x - relOffsets[y];
                    triangles[t + 1] = offsets[y] + x + 1 - relOffsets[y];
                    triangles[t + 0] = offsets[y + 1] + x + 1 - relOffsets[y + 1];
                    t += 3;
                    continue;
                }
                if (wl(x, y) >= 0 && wl(x, y + 1) >= 0 && wl(x + 1, y + 1) >= 0)
                {
                    triangles[t + 0] = offsets[y] + x - relOffsets[y];
                    triangles[t + 1] = offsets[y + 1] + x - relOffsets[y + 1];
                    triangles[t + 2] = offsets[y + 1] + x + 1 - relOffsets[y + 1];
                    t += 3;
                    continue;
                }
            }
        }
    }

    int calculateTriangleCount()
    {
        
        int tris = 0;
        for (int y = 1; y < WATER_SIZE_Y - 1; ++y)
            for (int x = 1; x < WATER_SIZE_X - 1; ++x)
            {
                var v = (((wl(x, y) >= 0) ? 1 : 0) +
                         ((wl(x + 1, y) >= 0) ? 1 : 0) +
                         ((wl(x, y + 1) >= 0) ? 1 : 0) +
                         ((wl(x + 1, y + 1) >= 0) ? 1 : 0));
                if (v == 4)
                    tris += 2;
                else if (v == 3)
                    tris++;
            }
        return tris;
    }

    void moveWater()
    {
        for (int y = 1; y < WATER_SIZE_Y - 1; y++)
            for (int x = 1; x < WATER_SIZE_X - 1; x++)
            {
                // detect the setup where there is no water
                if (wl(x - 1, y) == -1 && wl(x + 1, y) == -1 && wl(x, y + 1) == -1 && wl(x, y - 1) == -1)
                    continue;
                var alpha = 0.0002f;
                var diff = (wlT(x - 1, y) + wlT(x + 1, y) + wlT(x, y - 1) + wlT(x, y + 1) - 4 * wlT(x, y));
                float cur = wl(x, y);
                if (cur == -1)
                    cur = 0;
                float prev = waterLevelPrevStep[x + y * WATER_SIZE_X];
                if (prev == -1)
                    prev = 0;
                waterLevelStep[x + y * WATER_SIZE_X] = 2 * cur + alpha * diff - prev;
            }

        for (int y = 1; y < WATER_SIZE_Y; y++)
            for (int x = 1; x < WATER_SIZE_X; x++)
                waterLevelPrevStep[x + y * WATER_SIZE_X] = waterLevel[x + y * WATER_SIZE_X];
        float[] b = waterLevel;
        waterLevel = waterLevelStep;
        waterLevelStep = b;
    }

    void updateWaterTexture()
    {
        int start = 1, end = 199;
        int size = end - start;
        var vertices = new Vector3[size * size];
        var tris = new int[(size - 1) * (size - 1) * 6];

        int c = 0;
        int t = 0;
        for (int y = start; y < end; y++)
            for (int x = start; x < end; x++)
            {
                vertices[c] = new Vector3(x * Terrain.SCALE, wl(x, y) + terrainHeight(x, y), y * Terrain.SCALE);
                if (y < end - 1 && x < end - 1)
                {
                    int ix = x - start;
                    int iy = y - start;
                    tris[t] = ix + iy * size;
                    tris[t + 1] = ix + (iy + 1) * size;
                    tris[t + 2] = (ix + 1) + (iy + 1) * size;
                    tris[t + 3] = ix + iy * size;
                    tris[t + 4] = (ix + 1) + (iy + 1) * size;
                    tris[t + 5] = ix + 1 + iy * size;
                    t += 6;
                }
                c++;
            }

        var mesh = GetComponent<MeshFilter>().mesh;
        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return;
        /*int[] offsets = new int[WATER_SIZE_Y];
        int[] relOffsets = new int[WATER_SIZE_Y];
        var vCount = calculateVertexCount(offsets, relOffsets);
        var vertices = new Vector3[vCount];
        populateVertices(vertices);
        var tris = new int[calculateTriangleCount() * 6];
        populateTriangles(tris, offsets, relOffsets);

        var mesh = GetComponent<MeshFilter>().mesh;
        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        
        return;*/
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
