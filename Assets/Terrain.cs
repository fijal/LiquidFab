using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public int indexX, indexY;
    //public float updateTerrain;

    public void tileHit(int triIndex, bool mod)
    {
        var gridPosY = triIndex / 2 / 128;
        var gridPosX = (triIndex / 2) % 128;

        var ter = transform.parent.GetComponent<Terrain>();
        ter.terrainMod(indexX, indexY, gridPosX, gridPosY, mod ? -0.5f : 0.5f);
        //updateTerrain = 0.3f;
    }

    public void Update()
    {
        /*if (updateTerrain > 0)
        {
            updateTerrain -= Time.deltaTime;
            if (updateTerrain <= 0)
            {
                Debug.Log("updating terrain");
                updateTerrain = 0;
                var c = transform.parent.GetComponent<Terrain>().propagateTerrainChanges();
                if (c > 0)
                    updateTerrain = 0.3f;
            }
        }*/
    }
}

public class Terrain : MonoBehaviour
{
    Mesh mesh;
    public const float SCALE = 0.5f;
    public const float HEIGHT_SCALE = 40f;
    public const int TERRAIN_SIZE = 256;

    public Material terrainMat;
    public TextAsset terrainData;

    Simulation s;

    public NativeArray<float> terrainHeight;

    int updatingCountdown;

    public float height(int x, int y)
    {
        return terrainHeight[x + y * TERRAIN_SIZE];
    }

    public void setHeight(int x, int y, float v)
    {
        terrainHeight[x + y * TERRAIN_SIZE] = v;
    }

    public void terrainMod(int tileX, int tileY, int gridX, int gridY, float val)
    {
        var x = gridX + tileX * TERRAIN_SIZE;
        var y = gridY + tileY * TERRAIN_SIZE;

        setHeight(x, y, height(x, y) + val);
        updatingCountdown = 1;
        //recalculateMesh(tileX, tileY);
    }

    void recalculateMesh()
    {
        var mesh = createMesh();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    void Start()
    {
        var terrainDataBytes = terrainData.bytes;
        terrainHeight = new NativeArray<float>(TERRAIN_SIZE * TERRAIN_SIZE, Allocator.Persistent);
        for (int y = 0; y < TERRAIN_SIZE; y++)
            for (int x = 0; x < TERRAIN_SIZE; x++)
                terrainHeight[x + y * TERRAIN_SIZE] = ((float)terrainDataBytes[y + x * 512]) / 255 * HEIGHT_SCALE * SCALE;

        recalculateMesh();
        
        s = new Simulation(this, SimulationType.Terrain, terrainHeight, TERRAIN_SIZE, TERRAIN_SIZE);
        s.friction = 0.5f;
        s.viscosity = 0.1f;
        s.maxAngle = 0.2f;
        s.mass = 1f;
    }

    private void OnDestroy()
    {
        s.Dispose();
        if (terrainHeight != null)
            terrainHeight.Dispose();
    }

    Mesh createMesh()
    {
        mesh = new Mesh();
        
        var vertices = new Vector3[TERRAIN_SIZE * TERRAIN_SIZE];
        var triangles = new int[(TERRAIN_SIZE - 1) * (TERRAIN_SIZE - 1) * 6];
        
        for (int x = 0; x < TERRAIN_SIZE; ++x)
            for (int y = 0; y < TERRAIN_SIZE; ++y)
            {
                var h = height(x, y);
                vertices[x + y * TERRAIN_SIZE] = new Vector3(x * SCALE, h, y * SCALE);
            }
        int vert = 0;
        for (int tris = 0; tris < (TERRAIN_SIZE - 1) * (TERRAIN_SIZE - 1); tris++)
        {
            var t = tris * 6;
            triangles[t + 0] = vert + 0;
            triangles[t + 1] = vert + TERRAIN_SIZE;
            triangles[t + 2] = vert + 1;
            triangles[t + 3] = vert + 1;
            triangles[t + 4] = vert + TERRAIN_SIZE;
            triangles[t + 5] = vert + TERRAIN_SIZE + 1;
            vert++;
            if (tris % (TERRAIN_SIZE - 1) == TERRAIN_SIZE - 2)
                vert++;
        }
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    public void synchronizedUpdate()
    {
        if (updatingCountdown > 0)
        {
            updatingCountdown -= 1;
            if (updatingCountdown == 0)
            {
                //s.Step();
                recalculateMesh();
                updatingCountdown = 10;
            }
        }
    }
}
