using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;

public class Terrain : MonoBehaviour
{
    Mesh mesh;
    public const float SCALE = 0.5f;
    public const float HEIGHT_SCALE = 45f;
    public const int TERRAIN_SIZE = 256;

    public List<int> terrainUpdatesX, terrainUpdatesY;
    public List<float> terrainUpdatesVal;
    Dictionary<int, GameObject> trees;

    JobHandle? sjobhandle;

    public Material terrainMat;
    public TextAsset terrainData;
    public GameObject logPrefab;
    public GameObject[] treePrefabs;

    public Simulation s;
    public Water water;

    public float[] terrainHeight;
    public bool gameToLoad = false;

    float lastUpdate = 0.1f;

    public float height(int x, int y)
    {
        return terrainHeight[x + y * TERRAIN_SIZE];
    }

    public float heightFloat(float x, float y)
    {
        int ix = (int)x;
        float xrem = x - (float)ix;
        int iy = (int)y;
        float yrem = y - (float)iy;
        return ((1 - xrem) * height(ix + 1, iy) + (1 - yrem) * height(ix, iy + 1) + (xrem + yrem) * height(ix, iy)) / 2;
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

    public void spawnTree(int x, int y)
    {
        if (trees.ContainsKey(x + y * TERRAIN_SIZE))
            return;
        var go = Instantiate(treePrefabs[Random.Range(0, 3)], transform);
        //go.GetComponent<Log>().terrain = this;
        go.transform.position = new Vector3(x * SCALE, height(x, y), y * SCALE);
        //go.transform.rotation = Quaternion.Euler(90, 0, 0);
        go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        trees[x + y * TERRAIN_SIZE] = go;
    }

    public void spawnLog(int x, int y)
    {
        var go = Instantiate(logPrefab, transform);
        go.GetComponent<Log>().terrain = this;
        go.transform.position = new Vector3(x * SCALE, height(x, y), y * SCALE);
        go.transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    struct MeshBaker : IJob
    {
        public int mesh_id;

        public void Execute()
        {
            Physics.BakeMesh(mesh_id, convex: false);
        }
    }
    MeshBaker s_meshbaker;
    JobHandle? s_meshbaker_handle;
    Mesh s_meshbaker_mesh;

    void recalculateMesh()
    {
        var oldMesh = GetComponent<MeshFilter>().sharedMesh;
        if (oldMesh == null)
        {
            Mesh mesh = createMesh();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            GetComponent<MeshCollider>().sharedMesh = mesh;
        }
        else
        {
            FinishBakingMesh();
            updateMesh(oldMesh);
            s_meshbaker_mesh = oldMesh;
            s_meshbaker.mesh_id = oldMesh.GetInstanceID();
            s_meshbaker_handle = s_meshbaker.Schedule();
        }
    }

    void FinishBakingMesh()
    {
        if (s_meshbaker_handle != null)
        {
            s_meshbaker_handle.Value.Complete();
            s_meshbaker_handle = null;
            GetComponent<MeshCollider>().sharedMesh = s_meshbaker_mesh;
        }
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
        populateTrees();

        water = transform.Find("Water").GetComponent<Water>();
        trees = new Dictionary<int, GameObject>();
        s = new Simulation(water.waterLevel, TERRAIN_SIZE, TERRAIN_SIZE);
    }

    void populateTrees()
    {
        /*for (int i = 0; i < 3000; i++)
        {
            var tree = Instantiate(treePrefabs[Random.Range(0, 3)], transform);
            var x = Random.Range(0.5f, TERRAIN_SIZE * SCALE - 0.5f);
            var y = Random.Range(0.5f, TERRAIN_SIZE * SCALE - 0.5f);
            tree.transform.position = new Vector3(x, heightFloat(x / SCALE, y / SCALE), y);
            tree.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        }*/
    }

    private void OnDestroy()
    {
        FinishBakingMesh();
        sjobhandle?.Complete();
        s.Dispose();
    }

    Mesh createMesh()
    {
        mesh = new Mesh();
        mesh.MarkDynamic();

        var vertices0 = new Vector3[TERRAIN_SIZE * TERRAIN_SIZE];
        var triangles = new int[(TERRAIN_SIZE - 1) * (TERRAIN_SIZE - 1) * 6];

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
        mesh.vertices = vertices0;
        mesh.triangles = triangles;
        updateMesh(mesh);
        return mesh;
    }

    void updateMesh(Mesh mesh)
    {
        var vertices = new Vector3[TERRAIN_SIZE * TERRAIN_SIZE];
        for (int x = 0; x < TERRAIN_SIZE; ++x)
            for (int y = 0; y < TERRAIN_SIZE; ++y)
            {
                var h = height(x, y);
                vertices[x + y * TERRAIN_SIZE] = new Vector3(x * SCALE, h, y * SCALE);
            }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    public void runUpdates()
    {
        for (int i = 0; i < terrainUpdatesX.Count; i++)
        {
            var x = terrainUpdatesX[i];
            var y = terrainUpdatesY[i];
            setHeight(x, y, height(x, y) + terrainUpdatesVal[i]);
            setHeight(x, y + 1, height(x, y + 1) + terrainUpdatesVal[i]);
            setHeight(x + 1, y, height(x + 1, y) + terrainUpdatesVal[i]);
            setHeight(x + 1, y + 1, height(x + 1, y + 1) + terrainUpdatesVal[i]);
        }
        terrainUpdatesX.Clear();
        terrainUpdatesY.Clear();
        terrainUpdatesVal.Clear();

        List<int> treesToRemove = new List<int>();

        foreach (var entry in trees)
        {
            var x = entry.Key % TERRAIN_SIZE;
            var y = entry.Key / TERRAIN_SIZE;
            if (water.waterLevel[entry.Key] > 0.15f)
            {
                Destroy(entry.Value);
                spawnLog(x, y);
                treesToRemove.Add(entry.Key);
            }
        }
        for (int i = 0; i < treesToRemove.Count; i++)
            trees.Remove(treesToRemove[i]);
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

        if (s_meshbaker_handle != null && s_meshbaker_handle.Value.IsCompleted)
            FinishBakingMesh();
    }
}
