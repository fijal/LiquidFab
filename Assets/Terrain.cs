using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;
using System.Runtime.InteropServices;

public class Terrain : MonoBehaviour
{
    Mesh mesh;
    public const float SCALE = 0.5f;
    public const float HEIGHT_SCALE = 45f;
    public const int TERRAIN_SIZE = 256;

    public List<int> terrainUpdatesX, terrainUpdatesY;
    public List<float> terrainUpdatesVal;
    Dictionary<int, GameObject> trees;
    Dictionary<int, GameObject> waterPumps;

    readonly MyJobRunner s_runner = new();

    public TextAsset terrainData;
    public GameObject logPrefab, minerPrefab, magnetPrefab, waterPumpPrefab, smokePrefab;
    public GameObject[] treePrefabs;
    public GameObject infoDialog;

    List<GameObject> logs;

    public Simulation s;
    public Water water;

    public float[] terrainHeight;
    public float[] terrainKind;
    public bool gameToLoad = false;

    Texture2D terrainKindTexture;

    float lastUpdate = 0.1f;
    float subLevel;

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
        terrainUpdatesVal.Add(mod ? -2.5f * val : 2.5f * val);
    }

    public void spawnTree(int x, int y)
    {
        if (trees.ContainsKey(x + y * TERRAIN_SIZE))
            return;
        var go = Instantiate(treePrefabs[Random.Range(0, 3)], transform);
        go.transform.position = new Vector3((x + 0.5f) * SCALE, heightFloat(x + 0.5f, y + 0.5f), (y + 0.5f) * SCALE);
        go.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        go.AddComponent<Tree>();
        trees[x + y * TERRAIN_SIZE] = go;
    }

    public GameObject spawnLog(int x, int y)
    {
        var go = Instantiate(logPrefab, transform);
        go.GetComponent<Log2>().terrain = this;
        go.transform.position = new Vector3(x * SCALE, height(x, y), y * SCALE);
        go.transform.rotation = Quaternion.Euler(90, Random.Range(0, 90), 0);
        return go;
    }

    public void spawnMagnet(int x, int y)
    {
        var go = Instantiate(magnetPrefab, transform);
        go.transform.position = new Vector3(x * SCALE, height(x, y), y * SCALE);
        go.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
    }

    public void spawnMiner(int x, int y)
    {
        var go = Instantiate(minerPrefab, transform);
        go.transform.position = new Vector3(x * SCALE, height(x, y), y * SCALE);
        go.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
    }

    public bool spawnWaterPump(int x, int y)
    {
        if (water.waterSource.ContainsKey(x + y * TERRAIN_SIZE))
            return false;
        foreach (var entry in waterPumps)
        {
            var ex = entry.Key % TERRAIN_SIZE;
            var ey = entry.Key / TERRAIN_SIZE;
            if (Mathf.Abs(x - ex) < 5 && Mathf.Abs(y - ey) < 5)
                return false;
        }
        var go = Instantiate(waterPumpPrefab, transform);
        go.transform.position = new Vector3((x + 0.5f) * SCALE, heightFloat(x + 0.5f, y + 0.5f), (y + 0.5f) * SCALE);
        go.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        var smoke = Instantiate(smokePrefab, go.transform);
        smoke.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        smoke.transform.localPosition = new Vector3(0.03f, 0.8f, 0.2f);
        var wp = go.AddComponent<waterPump>();
        wp.basePos = heightFloat(x + 0.5f, y + 0.5f);
        water.waterSource[x + y * TERRAIN_SIZE] = 0.25f;
        waterPumps.Add(x + y * TERRAIN_SIZE, go);
        return true;
    }

    public void changeTerrainKind(int x, int y, int kind)
    {
        terrainKind[x + y * TERRAIN_SIZE] = kind;
        terrainKind[x + 1 + y * TERRAIN_SIZE] = kind;
        terrainKind[x + (y + 1) * TERRAIN_SIZE] = kind;
        terrainKind[x + 1 + (y + 1) * TERRAIN_SIZE] = kind;
    }

    struct MeshBaker : IJob
    {
        public int mesh_id;

        public void Execute()
        {
            Physics.BakeMesh(mesh_id, convex: false);
        }
    }
    MeshBaker meshbaker;
    readonly MyJobRunner meshbaker_runner = new();

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
            meshbaker_runner.Complete();
            updateMesh(oldMesh);
            updateTerrainKind();
            meshbaker.mesh_id = oldMesh.GetInstanceID();
            meshbaker_runner.Start(this, ref meshbaker, () =>
            {
                GetComponent<MeshCollider>().sharedMesh = oldMesh;
            });
        }
    }

    void Start()
    {
        var terrainDataBytes = terrainData.bytes;
        terrainHeight = new float[TERRAIN_SIZE * TERRAIN_SIZE];
        terrainKind = new float[TERRAIN_SIZE * TERRAIN_SIZE];
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

        logs = new List<GameObject>();

        recalculateMesh();
        populateTrees();
        createTerrainKindTexture();

        water = transform.Find("Water").GetComponent<Water>();
        trees = new Dictionary<int, GameObject>();
        waterPumps = new Dictionary<int, GameObject>();
        s = new Simulation(TERRAIN_SIZE, TERRAIN_SIZE);
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
        meshbaker_runner.Dispose();
        s_runner.Dispose();
        s.Dispose();
    }

    Mesh createMesh()
    {
        mesh = new Mesh();
        mesh.MarkDynamic();

        var vertices0 = new Vector3[TERRAIN_SIZE * TERRAIN_SIZE];
        var triangles = new int[(TERRAIN_SIZE - 1) * (TERRAIN_SIZE - 1) * 6];
        var uvs = new Vector2[TERRAIN_SIZE * TERRAIN_SIZE];

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
        for (int y = 0; y < TERRAIN_SIZE; y++)
            for (int x = 0; x < TERRAIN_SIZE; x++)
                uvs[x + y * TERRAIN_SIZE] = new Vector2(x, y);

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices0;
        mesh.triangles = triangles;
        mesh.uv = uvs;
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

    public void showTerrainInfo(GameObject camera, int x, int y)
    {
        var dialogContainer = infoDialog.transform.parent;

        infoDialog.SetActive(true);
        var d = infoDialog.GetComponent<Dialog>();
        d.terrain = this;
        d.x = x;
        d.y = y;
        var newPos = new Vector3(x * SCALE, camera.transform.position.y, y * SCALE);
        var distance = newPos - camera.transform.position;
        var rot = Quaternion.LookRotation(distance, Vector3.up).eulerAngles;
        infoDialog.transform.rotation = Quaternion.Euler(0, rot.y, rot.z);
        var scale = distance.magnitude;
        var FAC = 0.15f;
        dialogContainer.localScale = new Vector3(FAC * scale, FAC * scale, FAC * scale);
        dialogContainer.transform.position = new Vector3(x * SCALE, height(x, y), y * SCALE);
        updateDialog(x, y);
    }

    public void updateDialog(int x, int y)
    {
        infoDialog.GetComponent<Dialog>().text.text = $"X, Y: {x} {x}\nHeight: {height(x, y)}\nwater level {water.waterLevel[x + TERRAIN_SIZE * y]}\n" +
            $"water flow: {water.flowX(x, y)} {water.flowY(x, y)}\n" +
            $"sub level: {subLevel}";
    }

    void createTerrainKindTexture()
    {
        terrainKindTexture = new Texture2D(TERRAIN_SIZE, TERRAIN_SIZE, TextureFormat.RFloat, false, true);
        Shader.SetGlobalTexture("MTerrainKind", terrainKindTexture);
    }

    void updateTerrainKind()
    {
        terrainKindTexture.SetPixelData(terrainKind, 0);
        terrainKindTexture.Apply();
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
                //logs.Add(spawnLog(x, y));
                treesToRemove.Add(entry.Key);
            } else
            {
                s.subLevel[x + y * TERRAIN_SIZE] -= 0.003f;
                if (s.subLevel[x + y * TERRAIN_SIZE] < 0)
                    s.subLevel[x + y * TERRAIN_SIZE] = 0;
            }
        }
        for (int i = 0; i < treesToRemove.Count; i++)
            trees.Remove(treesToRemove[i]);

        for (int i = 0; i < logs.Count; i++)
            logs[i].GetComponent<Log2>().force = Vector3.zero;

        for (int i = 0; i < logs.Count; i++)
            for (int j = 0; j < logs.Count; j++)
                if (i != j)
                {
                    var rel = logs[i].transform.position - logs[j].transform.position;
                    var val = rel.sqrMagnitude;
                    if (val < 0.005f * 0.005f)
                        val = 0.005f * 0.005f;
                    var forceVal = 1 / val;
                    logs[i].GetComponent<Log2>().force += new Vector2(rel.x * forceVal, rel.z * forceVal);
                    logs[j].GetComponent<Log2>().force -= new Vector2(rel.x * forceVal, rel.z * forceVal);
                }
    }

    public void Update()
    {
        if (lastUpdate <= 0 && !s_runner.Running)
        {
            runUpdates();
            s.terrain.CopyFrom(terrainHeight);
            recalculateMesh();
            // XXX this is done 10 times per second only if the background thread can keep up;
            // otherwise this is done less often.  We should probably do waterLevel[] +=
            // some value computed from how long it really was since the last time we were here
            water.updateWaterSources();
            s.water.CopyFrom(water.waterLevel);
            s_runner.Start(this, ref s, () =>
            {
                s.water.CopyTo(water.waterLevel);
                s.waterFlowX.CopyTo(water.waterFlowX);
                s.waterFlowY.CopyTo(water.waterFlowY);
                s.terrain.CopyTo(terrainHeight);
                var d = infoDialog.GetComponent<Dialog>();
                subLevel = s.subLevel[d.x + d.y * TERRAIN_SIZE];
                water.updateWaterTexture(s);
            });
            lastUpdate = 0.1f;
        }
        else
        {
            lastUpdate -= Time.deltaTime;
        }
    }
}
