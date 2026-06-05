using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Terrain : MonoBehaviour
{
    Mesh mesh;
    int TILE_SIZE_X = 128;
    int TILE_SIZE_Y = 128;
    int TILES = 16;
    float scale = 0.5f;
    
    // Start is called before the first frame update
    void Start()
    {
        for (int x = 0; x < TILES; x++)
            for (int y = 0; y < TILES; y++)
                createTile(x, y);
    }

    Mesh createTile(int ofsX, int ofsY)
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        var map = Resources.Load($"terrain/tile{ofsX}_{ofsY}") as Texture2D;
        
        var vertices = new Vector3[(TILE_SIZE_X + 1) * (TILE_SIZE_Y + 1)];
        var triangles = new int[TILE_SIZE_X * TILE_SIZE_Y * 6];
        var height = map.GetPixels();

        for (int x = 0; x < TILE_SIZE_X + 1; ++x)
            for (int y = 0; y < TILE_SIZE_Y + 1; ++y)
            {
                var h = height[x + y * 512].r * 10;
                vertices[x + y * (TILE_SIZE_X + 1)] = new Vector3(x * scale, h, y * scale);
            }
        int vert = 0;
        for (int tris = 0; tris < TILE_SIZE_X * TILE_SIZE_Y; tris++)
        {
            var t = tris * 6;
            triangles[t + 0] = vert + 0;
            triangles[t + 1] = vert + TILE_SIZE_X + 1;
            triangles[t + 2] = vert + 1;
            triangles[t + 3] = vert + 1;
            triangles[t + 4] = vert + TILE_SIZE_X + 1;
            triangles[t + 5] = vert + TILE_SIZE_X + 2;
            vert++;
            if (tris % TILE_SIZE_X == TILE_SIZE_X - 1)
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
