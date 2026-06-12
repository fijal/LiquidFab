using System.Collections;
using System.Collections.Generic;
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
    int TILE_SIZE_X = 129;
    int TILE_SIZE_Y = 129;
    int TILES = 4;
    public const float SCALE = 0.5f;
    public const float HEIGHT_SCALE = 40f;
    public const int TERRAIN_SIZE = 512;

    public Material terrainMat;
    public TextAsset terrainData;

    Simulation s;

    public float[] terrainHeight;

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
        var x = gridX + tileX * (TILE_SIZE_X - 1);
        var y = gridY + tileY * (TILE_SIZE_Y - 1);

        setHeight(x, y, height(x, y) + val);
        updatingCountdown = 1;
        //recalculateMesh(tileX, tileY);
    }

    void recalculateMesh(int tileX, int tileY)
    {
        var mesh = createTile(tileX, tileY);
        transform.Find($"tile{tileX}_{tileY}").GetComponent<MeshFilter>().mesh = mesh;
        // NOTE: 'collider.sharedMesh = filter.sharedMesh' is missing so the collider
        // keeps using the original mesh, is that expected?
    }

    void Start()
    {
        var terrainDataBytes = terrainData.bytes;
        terrainHeight = new float[TERRAIN_SIZE * TERRAIN_SIZE];
        for (int y = 0; y < TERRAIN_SIZE; y++)
            for (int x = 0; x < TERRAIN_SIZE; x++)
                terrainHeight[x + y * TERRAIN_SIZE] = ((float)terrainDataBytes[y + x * TERRAIN_SIZE]) / 255 * HEIGHT_SCALE * SCALE;

        for (int x = 0; x < TILES; x++)
            for (int y = 0; y < TILES; y++)
            {
                var tile = new GameObject($"tile{x}_{y}");
                tile.transform.SetParent(transform);
                tile.transform.position = new Vector3((TILE_SIZE_X - 1) * x * SCALE, 0, (TILE_SIZE_Y - 1) * y * SCALE);
                //tile.transform.rotation = Quaternion.Euler(new Vector3(0, 270, 0));
                var r = tile.AddComponent<MeshRenderer>();
                r.material = terrainMat;
                var filter = tile.AddComponent<MeshFilter>();
                var collider = tile.AddComponent<MeshCollider>();
                var t = tile.AddComponent<Tile>();
                t.indexX = x;
                t.indexY = y;
                filter.mesh = createTile(x, y);
                collider.sharedMesh = filter.sharedMesh;
            }

        s = new Simulation(this, SimulationType.Terrain, terrainHeight, TERRAIN_SIZE, TERRAIN_SIZE);
        s.friction = 0.5f;
        s.viscosity = 0.1f;
        s.maxAngle = 0.2f;
        s.mass = 1f;
    }

    Mesh createTile(int ofsX, int ofsY)
    {
        mesh = new Mesh();
        
        var vertices = new Vector3[(TILE_SIZE_X) * (TILE_SIZE_Y)];
        var triangles = new int[(TILE_SIZE_X - 1) * (TILE_SIZE_Y - 1) * 6];
        
        for (int x = 0; x < TILE_SIZE_X; ++x)
            for (int y = 0; y < TILE_SIZE_Y; ++y)
            {
                var ix = x + ofsX * (TILE_SIZE_X - 1);
                if (ix == TERRAIN_SIZE)
                    ix = TERRAIN_SIZE - 1;
                var iy = y + ofsY * (TILE_SIZE_Y - 1);
                if (iy == TERRAIN_SIZE)
                    iy = TERRAIN_SIZE - 1;
                var h = height(ix, iy);
                vertices[x + y * (TILE_SIZE_X)] = new Vector3(x * SCALE, h, y * SCALE);
            }
        int vert = 0;
        for (int tris = 0; tris < (TILE_SIZE_X - 1) * (TILE_SIZE_Y - 1); tris++)
        {
            var t = tris * 6;
            triangles[t + 0] = vert + 0;
            triangles[t + 1] = vert + TILE_SIZE_X;
            triangles[t + 2] = vert + 1;
            triangles[t + 3] = vert + 1;
            triangles[t + 4] = vert + TILE_SIZE_X;
            triangles[t + 5] = vert + TILE_SIZE_X + 1;
            vert++;
            if (tris % (TILE_SIZE_X - 1) == TILE_SIZE_X - 2)
                vert++;
        }
        // ======>   mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
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
                s.Step();
                recalculateMesh(0, 0);
                updatingCountdown = 10;
            }
        }
    }
}
