using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Water : MonoBehaviour, ISimulation
{
    float[] waterLevel;
    
    const int WATER_SIZE_X = 200, WATER_SIZE_Y = 200;
    const int WATER_OFFSET_X = 130, WATER_OFFSET_Y = 130;
    Terrain terrain;

    float lastUpdate;
    Simulation s;

    // Start is called before the first frame update
    void Start()
    {
        var mesh = new Mesh();
        mesh.MarkDynamic();    // may help with meshes that are often updated
        GetComponent<MeshFilter>().mesh = mesh;
        
        waterLevel = new float[WATER_SIZE_X * WATER_SIZE_Y];
        
        lastUpdate = 0.0f;
        s = new Simulation(WATER_SIZE_X, WATER_SIZE_Y, 1, this);
        s.friction = 0f;
        s.viscosity = 0.1f;

        terrain = transform.parent.GetComponent<Terrain>();
    }

    float wl(int x, int y)
    {
        return waterLevel[x + y * WATER_SIZE_X];
    }

    public float readAtPos(int x, int y)
    {
        return wl(x, y) + terrain.height(x, y);
    }

    public float[] getData()
    {
        return waterLevel;
    }

    void swl(int x, int y, float val)
    {
        waterLevel[x + y * WATER_SIZE_X] = val;
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
                vertices[c] = new Vector3(x * Terrain.SCALE, wl(x, y) + terrain.height(x, y) - 0.001f, y * Terrain.SCALE);
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

        var mesh = GetComponent<MeshFilter>().sharedMesh;
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
            terrain.synchronizedUpdate();
            swl(130, 130, wl(130, 130) + 1f);
            swl(130, 131, wl(130, 131) + 1f);
            swl(131, 130, wl(131, 130) + 1f);
            swl(131, 131, wl(131, 131) + 1f);
            s.Step();
            updateWaterTexture();
            lastUpdate = 0.1f;
        } else
        {
            lastUpdate -= Time.deltaTime;
        }
    }
}
