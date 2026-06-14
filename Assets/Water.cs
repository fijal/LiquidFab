using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

public class Water : MonoBehaviour
{
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<float> waterLevel;
    
    const int WATER_SIZE_X = 256, WATER_SIZE_Y = 256;
    Terrain terrain;

    // XXX change this into a dictionary
    Dictionary<int, float> waterSource;
    
    // Start is called before the first frame update
    void Start()
    {
        var mesh = new Mesh();
        mesh.MarkDynamic();    // may help with meshes that are often updated
        GetComponent<MeshFilter>().mesh = mesh;

        waterLevel = new NativeArray<float>(WATER_SIZE_X * WATER_SIZE_Y, Allocator.Persistent);
        
        terrain = transform.parent.GetComponent<Terrain>();
    
        waterSource = new Dictionary<int, float>();
    }

    private void OnDestroy()
    {
        if (waterLevel != null)
            waterLevel.Dispose();
    }

    public float waterLevelFloat(float x, float y)
    {
        int ix = (int)x;
        float xrem = x - (float)ix;
        int iy = (int)y;
        float yrem = y - (float)iy;
        return ((1 - xrem) * waterLevel[ix + 1 + iy * WATER_SIZE_X] +
                (1 - yrem) * waterLevel[ix + (iy + 1) * WATER_SIZE_X] +
                (xrem + yrem) * waterLevel[ix + iy * WATER_SIZE_X]) / 2;
    }

    public void modifyWaterSource(int x, int y, bool mod, float val)
    {
        val *= 0.3f;
        var index = x + y * WATER_SIZE_X;
        if (!mod)
        {
            if (waterSource.ContainsKey(index))
                waterSource[index] += val;
            else
                waterSource[index] = val;
        } else
        {
            if (waterSource.ContainsKey(index))
            {
                waterSource[index] -= val;
                if (waterSource[index] <= 0)
                    waterSource.Remove(index);
            }
        }
    }

    public void updateWaterSources()
    {
        foreach (KeyValuePair<int, float> entry in waterSource)
            waterLevel[entry.Key] += entry.Value;
    }

    public void updateWaterTexture(Simulation s)
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
}
