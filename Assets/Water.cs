using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class Water : MonoBehaviour
{
    public float[] waterLevel, waterFlowX, waterFlowY;
    
    const int WATER_SIZE_X = Terrain.TERRAIN_SIZE, WATER_SIZE_Y = Terrain.TERRAIN_SIZE;
    Terrain terrain;

    void Start()
    {
        var mesh = new Mesh();
        mesh.MarkDynamic();    // may help with meshes that are often updated
        GetComponent<MeshFilter>().mesh = mesh;

        waterLevel = new float[Terrain.TERRAIN_SIZE * Terrain.TERRAIN_SIZE];
        waterFlowX = new float[(Terrain.TERRAIN_SIZE + 1) * Terrain.TERRAIN_SIZE];
        waterFlowY = new float[(Terrain.TERRAIN_SIZE + 1) * Terrain.TERRAIN_SIZE];

        //for (int i = 0; i < Terrain.TERRAIN_SIZE * Terrain.TERRAIN_SIZE; i++)
        //    waterLevel[i] = 1.0f;
        
        terrain = transform.parent.GetComponent<Terrain>();
        GetComponent<MeshCollider>().sharedMesh = terrain.createMesh(true);
    }

    private void Update()
    {
        for (int x = 0; x < Terrain.TERRAIN_SIZE; x++)
            for (int y = 0; y < Terrain.TERRAIN_SIZE; y++)
            {
                var z = terrain.height(x, y) + waterLevel[x + y * Terrain.TERRAIN_SIZE] + 0.5f;
                Debug.DrawLine(new Vector3(x * Terrain.SCALE, z, y * Terrain.SCALE),
                                new Vector3(x * Terrain.SCALE + flowX(x, y), z, y * Terrain.SCALE + flowY(x, y)));
                // Debug.DrawLine(new Vector3(0, 0, 0), new Vector3(10, 10, 10));
            }
    }

    public float flowX(int x, int y)
    {
        return waterFlowX[x + y * (Terrain.TERRAIN_SIZE + 1)];
    }

    public float flowY(int x, int y)
    {
        return waterFlowY[x + y * Terrain.TERRAIN_SIZE];
    }

    public float flowXfloat(float x, float y)
    {
        int ix = (int)x;
        if (ix < 0) ix = 0;
        else if (ix >= WATER_SIZE_X) ix = WATER_SIZE_X - 1;
        float xrem = x - (float)ix;
        int iy = (int)y;
        if (iy < 0) iy = 0;
        else if (iy >= WATER_SIZE_Y - 1) iy = WATER_SIZE_Y - 2;
        float yrem = y - (float)iy;
        int i = ix + iy * (WATER_SIZE_X + 1);
        return Mathf.Lerp(
            Mathf.Lerp(waterFlowX[i], waterFlowX[i + 1], xrem),
            Mathf.Lerp(waterFlowX[i + WATER_SIZE_X + 1], waterFlowX[i + 1 + WATER_SIZE_X + 1], xrem),
            yrem);
    }

    public float flowYfloat(float x, float y)
    {
        int ix = (int)x;
        if (ix < 0) ix = 0;
        else if (ix >= WATER_SIZE_X - 1) ix = WATER_SIZE_X - 2;
        float xrem = x - (float)ix;
        int iy = (int)y;
        if (iy < 0) iy = 0;
        else if (iy >= WATER_SIZE_Y) iy = WATER_SIZE_Y - 1;
        float yrem = y - (float)iy;
        int i = ix + iy * WATER_SIZE_X;
        return Mathf.Lerp(
            Mathf.Lerp(waterFlowY[i], waterFlowY[i + 1], xrem),
            Mathf.Lerp(waterFlowY[i + WATER_SIZE_X], waterFlowY[i + 1 + WATER_SIZE_X], xrem),
            yrem);
    }

    public float waterLevelFloat(float x, float y)
    {
        int ix = (int)x;
        if (ix < 0) ix = 0;
        else if (ix >= WATER_SIZE_X - 1) ix = WATER_SIZE_X - 2;
        float xrem = x - (float)ix;
        int iy = (int)y;
        if (iy < 0) iy = 0;
        else if (iy >= WATER_SIZE_Y - 1) iy = WATER_SIZE_Y - 2;
        float yrem = y - (float)iy;
        int i = ix + iy * WATER_SIZE_X;
        return Mathf.Lerp(
            Mathf.Lerp(waterLevel[i], waterLevel[i + 1], xrem),
            Mathf.Lerp(waterLevel[i + WATER_SIZE_X], waterLevel[i + 1 + WATER_SIZE_X], xrem),
            yrem);
    }

    /*public void modifyWaterSource(int x, int y, bool mod, float val)
    {
        val *= 0.3f;
        var index = x + y * WATER_SIZE_X;
        if (!mod)
        {
            if (waterSource.ContainsKey(index))
                waterSource[index].increase(val);
            else
            {
                var go = Instantiate(waterSourcePrefab, transform);
                go.transform.position = new Vector3(x * Terrain.SCALE, terrain.height(x, y), y * Terrain.SCALE);
                var ws = go.GetComponent<WaterSource>();
                ws.increase(val);
                waterSource[index] = ws;
            }
        } else
        {
            if (waterSource.ContainsKey(index))
            {
                var survives = waterSource[index].decrease(val);
                if (!survives)
                {
                    Destroy(waterSource[index].gameObject);
                    waterSource.Remove(index);
                }
            }
        }
    }*/

    public void updateTerrainKind()
    {
        // XXX this is done 10 times per second only if the background thread can keep up;
        // otherwise this is done less often.  We should probably do waterLevel[] +=
        // some value computed from how long it really was since the last time we were here

        var m = 0f;
        for (int y = 0; y < WATER_SIZE_Y; y++)
            for (int x = 0; x < WATER_SIZE_X; x++)
            {
                var c = terrain.s.subLevel[x + y * WATER_SIZE_X];
                if (c > m)
                    m = c;

                var k = terrain.terrainKind[x + y * WATER_SIZE_X];
                switch ((uint)k)
                {
                    case 0:
                        if (c > 0.001f)
                        {
                            /* turn sand into grass over a period of one second */
                            k += 0.101f;
                            if (k >= 1f)
                                k = 1.99f;
                        }
                        else
                            k = 0f;
                        break;

                    case 1:
                        if (c < 0.001f)
                        {
                            /* turn grass back to sand over a period of two seconds */
                            k -= 0.051f;
                            if (k < 1f)
                                k = 0f;
                        }
                        else
                            k = 1.99f;
                        break;
                }
                terrain.terrainKind[x + y * WATER_SIZE_X] = k;

                if (c > 0.003f && Random.Range(0f, 1f) > 0.999f && waterLevel[x + y * WATER_SIZE_X] < 0.0001f)
                    terrain.spawnTree(x, y);

            }
    }

    public void updateWaterTexture(NativeArray<float> new_water_level)
    {
        const float MIN_WATER = 0.001f;

        var vertices = new Vector3[WATER_SIZE_X * WATER_SIZE_Y];
        var uvs = new Vector3[WATER_SIZE_X * WATER_SIZE_Y];
        var tris = new List<int>();

        var flowX = waterFlowX;
        var flowY = waterFlowY;

        bool[] corners = new bool[WATER_SIZE_X * WATER_SIZE_Y];

        int c = 0;
        for (int y = 0; y < WATER_SIZE_Y; y++)
            for (int x = 0; x < WATER_SIZE_X; x++)
            {
                float fx = flowX[x + y * (WATER_SIZE_X + 1)] + flowX[x + 1 + y * (WATER_SIZE_X + 1)];
                float fy = flowY[x + y * WATER_SIZE_X] + flowX[x + (y + 1) * WATER_SIZE_X];

                /* use the mean value of the previous waterLevel and the new_water_level fresh
                 * from Simulation.cs */
                float h_old = waterLevel[x + y * WATER_SIZE_X];
                float h_new = new_water_level[x + y * WATER_SIZE_X];
                float h = (h_old + h_new) * 0.5f;
                uvs[c] = new Vector3(fx, h, fy);
                bool any_water = h_old >= MIN_WATER || h_new >= MIN_WATER;
                corners[x + y * WATER_SIZE_X] = any_water;
                if (!any_water)
                    h = 0f;

                /* XXX hack to prevent gaps between the terrain and the water at the border */
                if (x == 0 || y == 0 || x == WATER_SIZE_X - 1 || y == WATER_SIZE_Y - 1)
                    h = 0f;

                h += terrain.height(x, y);
                vertices[c] = new Vector3(x * Terrain.SCALE, h, y * Terrain.SCALE);
                c += 1;
            }

        for (int y = 0; y < WATER_SIZE_Y - 1; y++)
            for (int x = 0; x < WATER_SIZE_X - 1; x++)
            {
                int b = x + y * WATER_SIZE_X;
                bool corner00 = corners[b];
                bool corner10 = corners[b + 1];
                bool corner01 = corners[b + WATER_SIZE_X];
                bool corner11 = corners[b + 1 + WATER_SIZE_X];
                int total_corners =
                    (corner00 ? 1 : 0) +
                    (corner10 ? 1 : 0) +
                    (corner01 ? 1 : 0) +
                    (corner11 ? 1 : 0);

                if (total_corners == 0)
                    continue;
                if (total_corners == 3 && (!corner01 || !corner10))
                {
                    tris.Add(b);
                    tris.Add(b + WATER_SIZE_X);
                    tris.Add(b + 1 + WATER_SIZE_X);
                    tris.Add(b + 1 + WATER_SIZE_X);
                    tris.Add(b + 1);
                    tris.Add(b);
                }
                else
                {
                    tris.Add(b);
                    tris.Add(b + WATER_SIZE_X);
                    tris.Add(b + 1);
                    tris.Add(b + WATER_SIZE_X);
                    tris.Add(b + 1 + WATER_SIZE_X);
                    tris.Add(b + 1);
                }
            }

        var mesh = GetComponent<MeshFilter>().sharedMesh;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return;
    }
}
