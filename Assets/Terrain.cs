using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Terrain : MonoBehaviour
{
    Mesh mesh;
    int TILE_SIZE_X = 129;
    int TILE_SIZE_Y = 129;
    int TILES = 16;
    const float SCALE = 0.5f;
    const float HEIGHT_SCALE = 40f;

    public Material terrainMat;
    
    // Start is called before the first frame update
    void Start()
    {
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
                filter.mesh = createTile(x, y);
            }
    }

    Mesh createTile(int ofsX, int ofsY)
    {
        mesh = new Mesh();
        var map = Resources.Load<TextAsset>($"terrain/tile{ofsX}_{ofsY}");
        
        var vertices = new Vector3[(TILE_SIZE_X) * (TILE_SIZE_Y)];
        var triangles = new int[(TILE_SIZE_X - 1) * (TILE_SIZE_Y - 1) * 6];
        var height = map.bytes;
        
        for (int x = 0; x < TILE_SIZE_X; ++x)
            for (int y = 0; y < TILE_SIZE_Y; ++y)
            {
                var h = ((float)height[x + y * TILE_SIZE_X]) / 255 * HEIGHT_SCALE * SCALE;
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
