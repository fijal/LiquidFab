using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class Water : MonoBehaviour
{
    NativeArray<float> waterLevel;
    
    const int WATER_SIZE_X = 512, WATER_SIZE_Y = 512;
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
        s = new Simulation(terrain, SimulationType.Water, waterLevel, WATER_SIZE_X, WATER_SIZE_Y);
        s.friction = 0f;
        s.viscosity = 0.1f;
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

    void updateWaterTexture()
    {
        var vertices = new Vector3[WATER_SIZE_X * WATER_SIZE_Y];
        var uvs = new Vector2[WATER_SIZE_X * WATER_SIZE_Y];
        var tris = new List<int>();

        int c = 0;
        for (int y = 0; y < WATER_SIZE_Y; y++)
            for (int x = 0; x < WATER_SIZE_X; x++)
            {
                vertices[c] = new Vector3(x * Terrain.SCALE, wl(x, y) + terrain.height(x, y) - 0.001f, y * Terrain.SCALE);
                uvs[c] = new Vector2(0, wl(x, y));
                if (y < WATER_SIZE_Y - 1 && x < WATER_SIZE_X - 1)
                {
                    tris.Add(x + y * WATER_SIZE_X);
                    tris.Add(x + (y + 1) * WATER_SIZE_X);
                    tris.Add(x + 1 + y * WATER_SIZE_X);
                    tris.Add(x + (y + 1) * WATER_SIZE_X);
                    tris.Add((x + 1) + (y + 1) * WATER_SIZE_X);
                    tris.Add(x + 1 + y * WATER_SIZE_X);
                }
                c++;
            }

        var mesh = GetComponent<MeshFilter>().sharedMesh;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
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
        if (sjobhandle != null && sjobhandle.Value.IsCompleted)
        {
            sjobhandle.Value.Complete();
            sjobhandle = null;
            updateWaterTexture();
        }

        if (lastUpdate <= 0 && sjobhandle == null)
        {
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
