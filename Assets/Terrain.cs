using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public int indexX, indexY;
    public float updateTerrain;

    public void tileHit(int triIndex, bool mod)
    {
        var gridPosY = triIndex / 2 / 128;
        var gridPosX = (triIndex / 2) % 128;

        var ter = transform.parent.GetComponent<Terrain>();
        ter.terrainMod(indexX, indexY, gridPosX, gridPosY, mod ? -0.5f : 0.5f);
        updateTerrain = 0.3f;
    }

    public void Update()
    {
        if (updateTerrain > 0)
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
        }
    }
}

public class Terrain : MonoBehaviour
{
    Mesh mesh;
    int TILE_SIZE_X = 129;
    int TILE_SIZE_Y = 129;
    int TILES = 16;
    public const float SCALE = 0.5f;
    public const float HEIGHT_SCALE = 40f;
    public const int TERRAIN_SIZE = 2048;

    public Material terrainMat;
    public TextAsset terrainData;

    float[] terrainHeight;
    float[] terrainFlowX, terrainFlowY;

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
        recalculateMesh(tileX, tileY);
    }

    public int propagateTerrainChanges()
    {
        return 0;

        float clip(float v)
        {
            if (v < 0.3f && v > -0.3f)
                return 0;
            return v;
        }

        // deal with tiles and boundary conditions
        for (int y = 0; y < TERRAIN_SIZE; y++)
            for (int x = 0; x < TERRAIN_SIZE; x++) {
                terrainFlowX[x + y * TERRAIN_SIZE] = 0;
                terrainFlowY[x + y * TERRAIN_SIZE] = 0;
            }
        float C = 0.1f;

        for (int y = 1; y < TERRAIN_SIZE; y++)
            for (int x = 1; x < TERRAIN_SIZE; x++)
            {
                terrainFlowX[x + y * TERRAIN_SIZE] = clip(height(x - 1, y) - height(x, y)) * C;
                terrainFlowY[x + y * TERRAIN_SIZE] = clip(height(x, y - 1) - height(x, y)) * C;
                if (terrainFlowX[x + y * TERRAIN_SIZE] != 0)
                    Debug.Log($"X {x} {y} {terrainFlowX[x + y * TERRAIN_SIZE]}");
                if (terrainFlowY[x + y * TERRAIN_SIZE] != 0)
                    Debug.Log($"Y {x} {y} {terrainFlowY[x + y * TERRAIN_SIZE]}");
            }
        var something_changed = 0;
        for (int y = 0; y < TERRAIN_SIZE; y++)
            for (int x = 0; x < TERRAIN_SIZE; x++)
            {
                //if (terrainFlowX[x + y * TERRAIN_SIZE] != 0 || terrainFlowY[x + y * TERRAIN_SIZE] != 0)
                {
                    setHeight(x, y, height(x, y) + terrainFlowX[x + y * TERRAIN_SIZE] + terrainFlowY[x + y * TERRAIN_SIZE]);
                    something_changed = 1;
                }
            }

        //for (int y = 0; y < TILES; y++)
        //    for (int x = 0; x < TILES; x++)
        //    {
        recalculateMesh(0, 0);
        //    }
        return something_changed;
    }

    void recalculateMesh(int tileX, int tileY)
    {
        var mesh = createTile(tileX, tileY);
        transform.Find($"tile{tileX}_{tileY}").GetComponent<MeshFilter>().mesh = mesh;
    }

    void Start()
    {
        var terrainDataBytes = terrainData.bytes;
        terrainHeight = new float[TERRAIN_SIZE * TERRAIN_SIZE];
        terrainFlowX = new float[TERRAIN_SIZE * TERRAIN_SIZE];
        terrainFlowY = new float[TERRAIN_SIZE * TERRAIN_SIZE];
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
                collider.sharedMesh = filter.mesh;
            }
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
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
