using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;

public class Terrain : MonoBehaviour
{
    Mesh mesh;
    public const float SCALE = 0.5f;
    public const float HEIGHT_SCALE = 85f;
    public const int TERRAIN_SIZE = 256;

    public List<int> terrainUpdatesX, terrainUpdatesY;
    public List<float> terrainUpdatesVal;

    JobHandle? sjobhandle;

    public Material terrainMat;
    public TextAsset terrainData;
    public GameObject logPrefab;

    Simulation s;
    Water water;

    public float[] terrainHeight;
    public bool gameToLoad = false;

    float lastUpdate = 0.1f;

    public float height(int x, int y)
    {
        return terrainHeight[x + y * TERRAIN_SIZE];
    }

    public void setHeight(int x, int y, float v)
    {
        terrainHeight[x + y * TERRAIN_SIZE] = v;
    }

    public void terrainMod(int x, int y, bool mod, float val)
    {
        terrainUpdatesX.Add(x);
        terrainUpdatesY.Add(y);
        terrainUpdatesVal.Add(mod ? -5f * val : 5f * val);
    }

    public void spawnLog(int x, int y)
    {
        var go = Instantiate(logPrefab, transform);
        go.transform.position = new Vector3(x * SCALE, height(x, y) + 5, y * SCALE);
    }

    void recalculateMesh()
    {
        var mesh = createMesh();
        var oldMesh = GetComponent<MeshFilter>().sharedMesh;
        GetComponent<MeshFilter>().sharedMesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
        if (oldMesh != null)
            Destroy(oldMesh);
    }

    void Start()
    {
        var terrainDataBytes = terrainData.bytes;
        terrainHeight = new float[TERRAIN_SIZE * TERRAIN_SIZE];
        var index = 0;
        for (int y = 0; y < TERRAIN_SIZE; y++)
            for (int x = 0; x < TERRAIN_SIZE; x++)
            {
                terrainHeight[x + y * TERRAIN_SIZE] = (
                    (float)((terrainDataBytes[index + 1] << 8) | terrainDataBytes[index]) / (1 << 16) * HEIGHT_SCALE * SCALE);
                index += 2;
            }

        terrainUpdatesX = new List<int>();
        terrainUpdatesY = new List<int>();
        terrainUpdatesVal = new List<float>();
        recalculateMesh();

        water = transform.Find("Water").GetComponent<Water>();
        s = new Simulation(water.waterLevel, TERRAIN_SIZE, TERRAIN_SIZE);
    }

    private void OnDestroy()
    {
        sjobhandle?.Complete();
        s.Dispose();
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

    public void runUpdates()
    {
        for (int i = 0; i < terrainUpdatesX.Count; i++)
        {
            var x = terrainUpdatesX[i];
            var y = terrainUpdatesY[i];
            setHeight(x, y, height(x, y) + terrainUpdatesVal[i]);
        }
        terrainUpdatesX.Clear();
        terrainUpdatesY.Clear();
        terrainUpdatesVal.Clear();
    }

    public void Update()
    {

        if (sjobhandle != null && sjobhandle.Value.IsCompleted)
        {
            sjobhandle.Value.Complete();
            sjobhandle = null;
            s.terrain.CopyTo(terrainHeight);
            water.updateWaterTexture(s);
        }

        if (lastUpdate <= 0 && sjobhandle == null)
        {
            runUpdates();
            s.terrain.CopyFrom(terrainHeight);
            recalculateMesh();
            // XXX this is done 10 times per second only if the background thread can keep up;
            // otherwise this is done less often.  We should probably do waterLevel[] +=
            // some value computed from how long it really was since the last time we were here
            water.updateWaterSources();
            s.water = water.waterLevel;
            sjobhandle = s.Schedule();
            lastUpdate = 0.1f;
        }
        else
        {
            lastUpdate -= Time.deltaTime;
        }
    }
}
