using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Water : MonoBehaviour
{
    float[] waterLevel;
    public TextAsset terrainData;

    int foo = 0;

    const int WATER_SIZE_X = 200, WATER_SIZE_Y = 200;
    const int WATER_OFFSET_X = 150, WATER_OFFSET_Y = 150;

    // Start is called before the first frame update
    void Start()
    {
        waterLevel = new float[WATER_SIZE_X * WATER_SIZE_Y];
        
        for (int y = 0; y < WATER_SIZE_Y; y++)
            for (int x = 0; x < WATER_SIZE_X; x++)
            {
                waterLevel[x + y * WATER_SIZE_X] = -1;
            }
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
                swl(x + WATER_OFFSET_X, y + WATER_OFFSET_Y, 1.5f);
            swl(x + WATER_OFFSET_X, (WATER_OFFSET_Y - 1), 0);
            swl(x + WATER_OFFSET_X, WATER_OFFSET_Y + 10, 0);
        }
        for (int y = 0; y < 10; y++)
        {
            swl(WATER_OFFSET_X - 1, y + WATER_OFFSET_Y, 0);
            swl(WATER_OFFSET_X + 10, y + WATER_OFFSET_Y, 0);
        }
    }

    public float terrainHeight(int x, int y)
    {
        return ((float)terrainData.bytes[x + y * Terrain.TERRAIN_SIZE]) / 255 * Terrain.HEIGHT_SCALE * Terrain.SCALE;
    }

    float wl(int x, int y)
    {
        return waterLevel[x + y * WATER_SIZE_X];
    }

    void swl(int x, int y, float val)
    {
        waterLevel[x + y * WATER_SIZE_X] = val;
    }

    int calculateVertexCount(int[] offsets, int[] relOffsets)
    {
        for (int y = 0; y < WATER_SIZE_Y; y++)
            offsets[y] = -1;

        int vert = 0;
        for (int y = 0; y < WATER_SIZE_Y; ++y)
        {
            for (int x = 0; x < WATER_SIZE_X; ++x)
            {
                if (waterLevel[x + y * WATER_SIZE_X] >= 0)
                {
                    if (offsets[y] == -1)
                    {
                        relOffsets[y] = x;
                        offsets[y] = vert;
                    }
                    vert++;
                }
            }
        }
        return vert;
    }

    void populateVertices(Vector3[] vert)
    {
        int c = 0;
        for (int y = 0; y < WATER_SIZE_Y; ++y)
            for (int x = 0; x < WATER_SIZE_X; ++x)
                if (wl(x, y) >= 0)
                {
                    vert[c] = new Vector3(x * Terrain.SCALE, wl(x, y) + terrainHeight(x, y), y * Terrain.SCALE);
                    c++;
                }
    }

    void populateTriangles(int[] triangles, int[] offsets, int[] relOffsets)
    {
        var t = 0;
        for (int y = 1; y < WATER_SIZE_Y - 1; ++y)
        {
            for (int x = 1; x < WATER_SIZE_X - 1; ++x)
            {
                if (wl(x, y) >= 0 && wl(x + 1, y) >= 0 && wl(x, y + 1) >= 0 && wl(x + 1, y + 1) >= 0)
                {
                    triangles[t + 2] = offsets[y] + x - relOffsets[y];
                    triangles[t + 1] = offsets[y] + x + 1 - relOffsets[y];
                    triangles[t + 0] = offsets[y + 1] + x - relOffsets[y + 1];
                    triangles[t + 3] = offsets[y] + x + 1 - relOffsets[y];
                    triangles[t + 4] = offsets[y + 1] + x - relOffsets[y + 1];
                    triangles[t + 5] = offsets[y + 1] + x + 1 - relOffsets[y + 1];
                    t += 6;
                }
            }
        }
    }

    int calculateTriangleCount()
    {
        
        int tris = 0;
        for (int y = 1; y < WATER_SIZE_Y - 1; ++y)
            for (int x = 1; x < WATER_SIZE_X - 1; ++x)
            {
                var v = (((wl(x, y) >= 0) ? 1 : 0) +
                         ((wl(x + 1, y) >= 0) ? 1 : 0) +
                         ((wl(x, y + 1) >= 0) ? 1 : 0) +
                         ((wl(x + 1, y + 1) >= 0) ? 1 : 0));
                if (v == 4)
                    tris += 2;
                //else if (v == 3)
                //    tris++;
            }
        return tris;
    }

    void updateWaterTexture()
    {
        var mesh = new Mesh();

        int[] offsets = new int[WATER_SIZE_Y];
        int[] relOffsets = new int[WATER_SIZE_Y];
        var vCount = calculateVertexCount(offsets, relOffsets);
        var vertices = new Vector3[vCount];
        populateVertices(vertices);
        var tris = new int[calculateTriangleCount() * 6];
        populateTriangles(tris, offsets, relOffsets);
        
        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = mesh;

        return;
        
        /*var triangles = new int[(WATER_SIZE_X - 1) * (WATER_SIZE_Y - 1) * 6];
        var terrainHeight = terrainData.bytes;
        int terrainSize = (int)Mathf.Sqrt((float)terrainHeight.Length);

        var scale = Terrain.SCALE;
        var heightScale = Terrain.HEIGHT_SCALE;

        
        for (int x = 0; x < WATER_SIZE_X; ++x)
            for (int y = 0; y < WATER_SIZE_Y; ++y)
                if (waterLevel[x + y * WATER_SIZE_X] > 0)
                {

                }
        int vert = 0;
        for (int tris = 0; tris < (WATER_SIZE_X - 1) * (WATER_SIZE_Y - 1); tris++)
        {
            var t = tris * 6;
            triangles[t + 0] = vert + 0;
            triangles[t + 1] = vert + WATER_SIZE_X;
            triangles[t + 2] = vert + 1;
            triangles[t + 3] = vert + 1;
            triangles[t + 4] = vert + WATER_SIZE_X;
            triangles[t + 5] = vert + WATER_SIZE_X + 1;
            vert++;
            if (tris % (WATER_SIZE_X - 1) == WATER_SIZE_X - 2)
                vert++;
        }
        //mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();*/
    }

    private void FixedUpdate()
    {
        if (foo == 0)
        {
            updateWaterTexture();
            foo = 1;
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
