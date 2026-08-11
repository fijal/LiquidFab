using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;
using System.Runtime.InteropServices;
using LiquidFab;
using Google.FlatBuffers;
using System.IO;

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
    HashSet<GameObject> sources;
    HashSet<GameObject> buildings;
    HashSet<GameObject> floaters;

    readonly MyJobRunner s_runner = new();

    public TextAsset terrainData;
    // XXX this should go away at some point XXX
    public GameObject logPrefab, waterPumpPrefab, smokePrefab, ironPlatePrefab, waterWheelPrefab;
    public GameObject sourcePrefab;
    public GameObject doodad;
    public GameObject[] treePrefabs;
    public GameObject infoDialog;
    public Controls controls;
    public Items items;

    List<GameObject> logs;
    bool firstUpdate = true;

    public Simulation s;
    [HideInInspector] public Water water;

    [HideInInspector] public float[] terrainHeight;
    [HideInInspector] public float[] terrainKind;
    [HideInInspector] public float[] walls;
    [HideInInspector] public string gameToSave = "";
    [HideInInspector] public string gameToLoad = "";

    Texture2D terrainKindTexture;

    float lastUpdate = 0.1f;
    float subLevel;

    public float height(int x, int y)
    {
        return terrainHeight[x + y * TERRAIN_SIZE];
    }

    public float heightWater(int x, int y)
    {
        return height(x, y) + water.waterLevel[x + y * TERRAIN_SIZE];
    }

    public float heightWaterFloat(float x, float y)
    {
        return heightFloat(x, y) + water.waterLevelFloat(x, y);
    }

    public float heightFloat(float x, float y)
    {
        int ix = (int)x;
        if (ix < 0) ix = 0;
        else if (ix >= TERRAIN_SIZE - 1) ix = TERRAIN_SIZE - 2;
        float xrem = x - (float)ix;
        int iy = (int)y;
        if (iy < 0) iy = 0;
        else if (iy >= TERRAIN_SIZE - 1) iy = TERRAIN_SIZE - 2;
        float yrem = y - (float)iy;
        int i = ix + iy * TERRAIN_SIZE;
        return Mathf.Lerp(
            Mathf.Lerp(terrainHeight[i], terrainHeight[i + 1], xrem),
            Mathf.Lerp(terrainHeight[i + TERRAIN_SIZE], terrainHeight[i + 1 + TERRAIN_SIZE], xrem),
            yrem);
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

    public void spawnTree(int x, int y, float age=0, int kind=-1)
    {
        if (trees.ContainsKey(x + y * TERRAIN_SIZE))
            return;
        if (kind == -1)
            kind = Random.Range(0, 3);
        var prefab = treePrefabs[kind];
        var go = Instantiate(prefab, transform);
        go.layer = 3; // terrain
        go.transform.position = new Vector3((x + 0.5f) * SCALE, heightFloat(x + 0.5f, y + 0.5f), (y + 0.5f) * SCALE);
        go.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        var t = go.AddComponent<Tree>();
        var bc = go.AddComponent<BoxCollider>();
        t.age = age;
        t.x = x;
        t.y = y;
        t.kind = kind;
        trees[x + y * TERRAIN_SIZE] = go;
    }

    public void spawnFloater(Vector3 loc, ItemType tp)
    {
        var itemSpec = items.items[tp];
        var go = Instantiate(itemSpec.prefab, transform);
        //Vector3 loc = spawner.GetComponent<Building>().spawnPoint.transform.position;
        go.transform.position = loc + new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));
        go.transform.rotation = Quaternion.Euler(0, Random.Range(0, 90), 0);
        go.GetComponent<Floater>().tp = tp;
        go.GetComponent<Floater>().terrain = this;
        floaters.Add(go);
    }

    public void spawnWaterWheel(Vector3 point, Quaternion rot)
    {
        spawnBuilding(waterWheelPrefab, point, rot);
    }

    public GameObject spawnBuilding(GameObject prefab, Vector3 point, Quaternion rot, BuildingSpec spec=null)
    {
        var go = Instantiate(prefab, transform);
        go.transform.position = point;
        go.transform.rotation = rot;
        if (go.GetComponent<Building>() != null)
            go.GetComponent<Building>().terrain = this;
        buildings.Add(go);
        if (spec != null) {
            if (go.GetComponent<Building>() != null)
                go.GetComponent<Building>().populateFromSpec(spec);
            else
                go.GetComponent<Construction>().populateFromSpec(spec);
        }
        return go;
    }

    public GameObject spawnLog(int x, int y)
    {
        var go = Instantiate(logPrefab, transform);
        go.GetComponent<Log2>().terrain = this;
        go.transform.position = new Vector3(x * SCALE, height(x, y), y * SCALE);
        go.transform.rotation = Quaternion.Euler(90, Random.Range(0, 90), 0);
        return go;
    }

    public void spawnWaterPump(GameObject prefab, Vector3 point, Quaternion rot)
    {
        spawnBuilding(prefab, point, rot);
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
    MeshBaker meshbaker, waterBaker;
    readonly MyJobRunner meshbaker_runner = new();
    readonly MyJobRunner waterbaker_runner = new();


    void recalculateMesh()
    {
        var oldMesh = GetComponent<MeshCollider>().sharedMesh;
        Debug.Assert(oldMesh != null);
        
        meshbaker_runner.Complete();
        updateMesh(oldMesh);
        updateTerrainKind();
        meshbaker.mesh_id = oldMesh.GetInstanceID();
        meshbaker_runner.Start(this, ref meshbaker, () =>
        {
            GetComponent<MeshCollider>().sharedMesh = oldMesh;
        });
    }

    void recalculateWaterMesh()
    {
        var oldMesh = water.GetComponent<MeshCollider>().sharedMesh;
        Debug.Assert(oldMesh != null);

        waterbaker_runner.Complete();
        updateMesh(oldMesh, true);
        water.updateTerrainKind();
        waterBaker.mesh_id = oldMesh.GetInstanceID();
        waterbaker_runner.Start(this, ref waterBaker, () =>
        {
            water.GetComponent<MeshCollider>().sharedMesh = oldMesh;
        });
    }

    void Start()
    {
        var terrainDataBytes = terrainData.bytes;
        terrainHeight = new float[TERRAIN_SIZE * TERRAIN_SIZE];
        terrainKind = new float[TERRAIN_SIZE * TERRAIN_SIZE];
        walls = new float[TERRAIN_SIZE * TERRAIN_SIZE];
        var index = 0;
        for (int y = 0; y < TERRAIN_SIZE; y++)
            for (int x = 0; x < TERRAIN_SIZE; x++)
            {
                terrainHeight[x + y * TERRAIN_SIZE] = 0;
                //terrainHeight[x + y * TERRAIN_SIZE] = (
                //    (float)((terrainDataBytes[index + 1] << 8) | terrainDataBytes[index]) / (1 << 16) * HEIGHT_SCALE * SCALE);
                index += 2;
            }

        spawnHill(64, 64);
        spawnHill(192, 64);
        spawnHill(192, 192);

        terrainUpdatesX = new List<int>();
        terrainUpdatesY = new List<int>();
        terrainUpdatesVal = new List<float>();

        logs = new List<GameObject>();
        
        water = transform.Find("Water").GetComponent<Water>();
        Mesh mesh = createMesh();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
        trees = new Dictionary<int, GameObject>();
        buildings = new HashSet<GameObject>();
        floaters = new HashSet<GameObject>();
        //populateTrees();
        createTerrainKindTexture();

        waterPumps = new Dictionary<int, GameObject>();
        sources = new HashSet<GameObject>();
        addMineralSource(100, 100);
        addMineralSource(104, 120);
        addMineralSource(120, 115);
        addMineralSource(100, 200);

        var baseObject = transform.Find("base").gameObject;
        buildings.Add(baseObject);

        s = new Simulation(TERRAIN_SIZE, TERRAIN_SIZE);
    }

    void spawnHill(int x, int y)
    {
        for (int iy = y - 20; iy < y + 20; iy++)
            for (int ix = x - 20; ix < x + 20; ix++)
            {
                float distance = Mathf.Sqrt((ix - x) * (ix - x) + (iy - y) * (iy - y));
                //Debug.Log(distance / 40);
                terrainHeight[ix + iy * TERRAIN_SIZE] = Mathf.Max(Mathf.Cos(distance / 20) * 10 - 5, 0);
            }
    }

    void addMineralSource(float x, float y)
    {
        var go = Instantiate(sourcePrefab, transform);
        // water level will fix itself next simulation frame 
        go.transform.position = new Vector3(x * Terrain.SCALE, heightFloat(x, y) + 0.1f, y * Terrain.SCALE);
        go.GetComponent<MineralSource>().terrain = this;
        sources.Add(go);
    }

    void populateTrees()
    {
        //for (int i = 0; i < 100; i++)
        //    spawnTree(Random.Range(0, TERRAIN_SIZE - 1), Random.Range(0, TERRAIN_SIZE - 1), Tree.MAX_AGE, 4);
    }

    private void OnDestroy()
    {
        meshbaker_runner.Dispose();
        waterbaker_runner.Dispose();
        s_runner.Dispose();
        s.Dispose();
    }

    public Mesh createMesh(bool isCollider = false)
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
        updateMesh(mesh, isCollider);
        return mesh;
    }

    public void updateMesh(Mesh mesh, bool isCollider = false)
    {
        var vertices = new Vector3[TERRAIN_SIZE * TERRAIN_SIZE];
        for (int x = 0; x < TERRAIN_SIZE; ++x)
            for (int y = 0; y < TERRAIN_SIZE; ++y)
            {
                var h = height(x, y);
                if (isCollider)
                    h += water.waterLevel[x + y * TERRAIN_SIZE];
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

    public void interactWithTerrain(int x, int y)
    {

        /*if (trees.ContainsKey(x + y * TERRAIN_SIZE))
        {
            var tree = trees[x + y * TERRAIN_SIZE];
            if (tree.GetComponent<Tree>().age < Tree.MAX_AGE)
                controls.showTooltip("tree too young");
            else
            {
                trees.Remove(x + y * TERRAIN_SIZE);
                Destroy(tree);
                controls.changeMouseCursorToLog();
            }
        }*/
        //Debug.Log($"{x} {y} {water.flowX(x, y)} {water.flowY(x, y)}");
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
                logs.Add(spawnLog(x, y));
                treesToRemove.Add(entry.Key);
            }
            else
            {
                s.subLevel[x + y * TERRAIN_SIZE] -= 0.003f;
                if (s.subLevel[x + y * TERRAIN_SIZE] < 0)
                    s.subLevel[x + y * TERRAIN_SIZE] = 0;
            }
        }
        for (int i = 0; i < treesToRemove.Count; i++)
            trees.Remove(treesToRemove[i]);

        foreach (var f in buildings)
        {
            if (BuildingHelper.getKind(f) == BuildingKind.waterPump)
            {
                var x = (int)(f.transform.position.x / SCALE);
                var y = (int)(f.transform.position.z / SCALE);
                water.waterLevel[x + y * TERRAIN_SIZE] += 1.0f;
            }
        }
        updateBuildings();
        updateSources();
    }

    public void updateBuildings()
    {
        foreach (var f in buildings)
        {
            var cur = f.transform.position;
            if (BuildingHelper.getKind(f) != BuildingKind.fence)
                f.transform.position = new Vector3(cur.x, heightWaterFloat(cur.x / SCALE, cur.z / SCALE), cur.z);
        }
    }

    public void updateSources()
    {
        foreach (var f in sources)
        {
            var cur = f.transform.position;
            f.transform.position = new Vector3(cur.x, heightWaterFloat(cur.x / SCALE, cur.z / SCALE) + 0.1f, cur.z);
        }
    }

    public void followHover(GameObject highlight, Vector3 hitPoint, bool followTerrain = false)
    {
        if (!followTerrain)
            highlight.transform.position = hitPoint;
        else
        {
            var h = heightFloat(hitPoint.x / SCALE, hitPoint.z / SCALE);
            highlight.transform.position = new Vector3(hitPoint.x, h, hitPoint.z);
        }
    }

    public void removeFloater(GameObject floater)
    {
        floaters.Remove(floater);
        Destroy(floater);
    }

    public void removeBuilding(GameObject building)
    {
        if (building.GetComponent<Building>() != null && building.GetComponent<Building>().kind == BuildingKind.fence)
            removeWall(building.transform.position.x / SCALE, building.transform.position.z / SCALE);
        buildings.Remove(building);
        Destroy(building);
    }

    void createInitialBuildings()
    {
        var assemblerSpec = (controls.tools.allTools["Assembler"] as BuildingFreePlacement).spec;
        var forgeSpec = (controls.tools.allTools["Forge"] as BuildingFreePlacement).spec;
        spawnBuilding(assemblerSpec.prefab, new Vector3(95 * SCALE, 1f, 110 * SCALE), Quaternion.Euler(0, 0, 0), assemblerSpec);
        spawnBuilding(forgeSpec.prefab, new Vector3(90 * SCALE, 1f, 110 * SCALE), Quaternion.Euler(0, 0, 0), forgeSpec);
    }

    public void markWall(float x, float y)
    {
        walls[(int)x + (int)y * TERRAIN_SIZE] = 1;
        walls[(int)(x + 1) + (int)y * TERRAIN_SIZE] = 1;
        walls[(int)x + (int)(y + 1) * TERRAIN_SIZE] = 1;
        walls[(int)(x + 1) + (int)(y + 1) * TERRAIN_SIZE] = 1;
    }

    void removeWall(float x, float y)
    {
        walls[(int)x + (int)y * TERRAIN_SIZE] = 0;
        walls[(int)(x + 1) + (int)y * TERRAIN_SIZE] = 0;
        walls[(int)x + (int)(y + 1) * TERRAIN_SIZE] = 0;
        walls[(int)(x + 1) + (int)(y + 1) * TERRAIN_SIZE] = 0;
    }

    public void Update()
    {
        if (firstUpdate)
        {
            createInitialBuildings();
            firstUpdate = false;
        }
        if (lastUpdate <= 0 && !s_runner.Running)
        {
            if (gameToLoad != "")
            {
                load(gameToLoad);
                gameToLoad = "";
                controls.doneSaveLoad();
            }
            if (gameToSave != "")
            {
                save(gameToSave);
                gameToSave = "";
                controls.doneSaveLoad();
            }
            runUpdates();
            s.terrain.CopyFrom(terrainHeight);
            recalculateMesh();
            recalculateWaterMesh();
            s.water.CopyFrom(water.waterLevel);
            s.waterFlowX.CopyFrom(water.waterFlowX);
            s.waterFlowY.CopyFrom(water.waterFlowY);
            s.walls.CopyFrom(walls);
            s_runner.Start(this, ref s, () =>
            {
                s.waterFlowX.CopyTo(water.waterFlowX);
                s.waterFlowY.CopyTo(water.waterFlowY);
                s.terrain.CopyTo(terrainHeight);
                var d = infoDialog.GetComponent<Dialog>();
                subLevel = s.subLevel[d.x + d.y * TERRAIN_SIZE];
                water.updateWaterTexture(s.water);
                s.water.CopyTo(water.waterLevel);
            });
            lastUpdate = 0.1f;
        }
        else
        {
            lastUpdate -= Time.deltaTime;
        }
    }

    public void save(string filename)
    {
        return; // broken and not working
        var builder = new FlatBufferBuilder(1024);
        
        LiquidFab.Savegame.Savegame.StartTreesVector(builder, trees.Count);
        foreach (var entry in trees)
        {
            var t = entry.Value.GetComponent<Tree>();
            LiquidFab.Savegame.Tree.CreateTree(builder, t.x, t.y, t.kind, t.age);
        }
        VectorOffset treeTable = builder.EndVector();

        LiquidFab.Savegame.Savegame.StartWaterPumpsVector(builder, waterPumps.Count);
        foreach (var entry in waterPumps)
        {
            var wp = entry.Value.GetComponent<waterPump>();
            var xy = entry.Key;
            var x = xy % TERRAIN_SIZE;
            var y = xy / TERRAIN_SIZE;
            //LiquidFab.Savegame.WaterPump.CreateWaterPump(builder, x, y, wp.fuelLevel, wp.logs);
        }
        VectorOffset waterPumpTable = builder.EndVector();

        VectorOffset terrainLevelTable = LiquidFab.Savegame.Savegame.CreateTerrainLevelVector(builder, terrainHeight);
        VectorOffset terrainKindTable = LiquidFab.Savegame.Savegame.CreateTerrainKindVector(builder, terrainKind);

        VectorOffset waterLevelTable = LiquidFab.Savegame.Savegame.CreateWaterLevelVector(builder, water.waterLevel);
        VectorOffset waterFlowX = LiquidFab.Savegame.Savegame.CreateWaterFlowXVector(builder, water.waterFlowX);
        VectorOffset waterFlowY = LiquidFab.Savegame.Savegame.CreateWaterFlowXVector(builder, water.waterFlowY);

        var buf = new float[TERRAIN_SIZE * TERRAIN_SIZE];
        s.subLevel.CopyTo(buf);
        VectorOffset subLevelTable = LiquidFab.Savegame.Savegame.CreateWaterLevelVector(builder, buf);
        buf = new float[(TERRAIN_SIZE + 1) * TERRAIN_SIZE];
        s.subFlowX.CopyTo(buf);
        VectorOffset subFlowX = LiquidFab.Savegame.Savegame.CreateSubFlowXVector(builder, buf);
        s.subFlowY.CopyTo(buf);
        VectorOffset subFlowY = LiquidFab.Savegame.Savegame.CreateSubFlowYVector(builder, buf);

        LiquidFab.Savegame.Savegame.StartSavegame(builder);
        LiquidFab.Savegame.Savegame.AddVersion(builder, Controls.SAVEGAME_VERSION);
        LiquidFab.Savegame.Savegame.AddTrees(builder, treeTable);
        LiquidFab.Savegame.Savegame.AddWaterPumps(builder, waterPumpTable);
        LiquidFab.Savegame.Savegame.AddTerrainLevel(builder, terrainLevelTable);
        LiquidFab.Savegame.Savegame.AddTerrainKind(builder, terrainKindTable);
        LiquidFab.Savegame.Savegame.AddWaterLevel(builder, waterLevelTable);
        LiquidFab.Savegame.Savegame.AddWaterFlowX(builder, waterFlowX);
        LiquidFab.Savegame.Savegame.AddWaterFlowY(builder, waterFlowY);
        LiquidFab.Savegame.Savegame.AddSubWaterLevel(builder, subLevelTable);
        LiquidFab.Savegame.Savegame.AddSubFlowX(builder, subFlowX);
        LiquidFab.Savegame.Savegame.AddSubFlowY(builder, subFlowY);
        var save_ofs = LiquidFab.Savegame.Savegame.EndSavegame(builder);
     
        builder.Finish(save_ofs.Value);
        File.WriteAllBytes(filename, builder.SizedByteArray());
    }

    public void clearLevel()
    {
        foreach (var entry in trees)
        {
            Destroy(entry.Value);
        }
        trees.Clear();
        foreach (var entry in waterPumps)
            Destroy(entry.Value);
        waterPumps.Clear();
    }

    public bool load(string savefile)
    {
        return false; // XXX broken and not working XXX
        byte[] bytes = File.ReadAllBytes(savefile);
        var save = LiquidFab.Savegame.Savegame.GetRootAsSavegame(new ByteBuffer(bytes));
        if (save.Version != Controls.SAVEGAME_VERSION)
        {
            controls.showTooltip("wrong savegame version!");
            return false;
        }

        clearLevel();
        var treesLen = save.TreesLength;
        for (int i = 0; i < treesLen; i++)
        {
            var t = save.Trees(i).Value;
            spawnTree(t.X, t.Y, t.Age, t.Kind);
        }
        var wpLen = save.WaterPumpsLength;
        for (int i = 0; i < wpLen; i++)
        {
            var wp = save.WaterPumps(i).Value;
            /*var p = spawnWaterPump(wp.X, wp.Y);
            p.fuelLevel = wp.FuelLevel;
            p.logs = wp.Logs;
            p.maybeConsumeLog();*/
        }
        var buf = new float[TERRAIN_SIZE * TERRAIN_SIZE];
        for (int i = 0; i < TERRAIN_SIZE * TERRAIN_SIZE; ++i)
        {
            water.waterLevel[i] = save.WaterLevel(i);
            water.waterFlowX[i] = save.WaterFlowX(i);
            water.waterFlowY[i] = save.WaterFlowY(i);
            terrainHeight[i] = save.TerrainLevel(i);
            terrainKind[i] = save.TerrainKind(i);
            buf[i] = save.SubWaterLevel(i);
        }
        s.water.CopyFrom(water.waterLevel);
        s.waterFlowX.CopyFrom(water.waterFlowX);
        s.waterFlowY.CopyFrom(water.waterFlowY);
        s.subLevel.CopyFrom(buf);
        buf = new float[TERRAIN_SIZE * (TERRAIN_SIZE + 1)];
        for (int i = 0; i < (TERRAIN_SIZE + 1) * TERRAIN_SIZE; ++i)
            buf[i] = save.SubFlowX(i);
        s.subFlowX.CopyFrom(buf);
        for (int i = 0; i < (TERRAIN_SIZE + 1) * TERRAIN_SIZE; ++i)
            buf[i] = save.SubFlowY(i);
        s.subFlowY.CopyFrom(buf);
        return true;
    }


#if UNITY_EDITOR
    private void LateUpdate()
    {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.position.sqrMagnitude > 20000f*20000f)
            {
                Debug.LogAssertion("TOO FAR AWAY: " + t.gameObject.name, t.gameObject);
                Debug.DebugBreak();
            }
        }
    }
#endif
}
