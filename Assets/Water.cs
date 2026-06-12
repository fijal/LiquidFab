using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class Water : MonoBehaviour
{
    NativeArray<float> waterLevel;
    
    const int WATER_SIZE_X = 256, WATER_SIZE_Y = 256;
    Terrain terrain;

    float lastUpdate;
    Simulation s;
    JobHandle? sjobhandle;

    // Start is called before the first frame update
    void Start()
    {
        var mesh = new Mesh();
        mesh.MarkDynamic();    // may help with meshes that are often updated
        GetComponent<MeshFilter>().mesh = mesh;

        waterLevel = new NativeArray<float>(WATER_SIZE_X * WATER_SIZE_Y, Allocator.Persistent);
        
        lastUpdate = 0.0f;
        terrain = transform.parent.GetComponent<Terrain>();
        s = new Simulation(waterLevel, WATER_SIZE_X, WATER_SIZE_Y);
 
    }

    private void OnDestroy()
    {
        s.Dispose();
        if (waterLevel != null)
            waterLevel.Dispose();
    }

    float wl(int x, int y)
    {
        return waterLevel[x + y * WATER_SIZE_X];
    }

    void swl(int x, int y, float val)
    {
        waterLevel[x + y * WATER_SIZE_X] = val;
    }

    public void updateWaterTexture()
    {
        const float MIN_WATER = 0.001f;

        var vertices = new Vector3[WATER_SIZE_X * WATER_SIZE_Y];
        var uvs = new Vector3[WATER_SIZE_X * WATER_SIZE_Y];
        var tris = new List<int>();

        var flowX = s.GetFlowX();
        var flowY = s.GetFlowY();

        int c = 0;
        for (int y = 0; y < WATER_SIZE_Y; y++)
            for (int x = 0; x < WATER_SIZE_X; x++)
            {
                float fx = flowX[x + y * (WATER_SIZE_X + 1)] + flowX[x + 1 + y * (WATER_SIZE_X + 1)];
                float fy = flowY[x + y * WATER_SIZE_X] + flowX[x + (y + 1) * WATER_SIZE_X];

                float h = waterLevel[x + y * WATER_SIZE_X];
                uvs[c] = new Vector3(fx, h, fy);
                if (h < MIN_WATER)
                    h = -0.2f;

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
                bool corner00 = waterLevel[b] >= MIN_WATER;
                bool corner10 = waterLevel[b + 1] >= MIN_WATER;
                bool corner01 = waterLevel[b + WATER_SIZE_X] >= MIN_WATER;
                bool corner11 = waterLevel[b + 1 + WATER_SIZE_X] >= MIN_WATER;
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

    // Update is called once per frame
    void Update()
    {
        if (sjobhandle != null && sjobhandle.Value.IsCompleted)
        {
            sjobhandle.Value.Complete();
            sjobhandle = null;
            for (int i = 0; i < Terrain.TERRAIN_SIZE * Terrain.TERRAIN_SIZE; i++)
                terrain.terrainHeight[i] = s.terrain[i];
            updateWaterTexture();
        }

        if (lastUpdate <= 0 && sjobhandle == null)
        {
            terrain.runUpdates();
            for (int i = 0; i < Terrain.TERRAIN_SIZE * Terrain.TERRAIN_SIZE; i++)
                s.terrain[i] = terrain.terrainHeight[i];
            terrain.synchronizedUpdate();
            swl(130, 130, wl(130, 130) + 1f);
            swl(130, 131, wl(130, 131) + 1f);
            swl(131, 130, wl(131, 130) + 1f);
            swl(131, 131, wl(131, 131) + 1f);
            sjobhandle = s.Schedule();
            lastUpdate = 0.1f;
        } else
        {
            lastUpdate -= Time.deltaTime;
        }
    }
}
