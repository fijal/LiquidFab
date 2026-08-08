using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Google.FlatBuffers;
using System.IO;


public class Controls : MonoBehaviour
{
    public new GameObject camera;
    public Terrain terrain;

    const float MOVE_SPEED = 10f;
    const float SHIFT_SPEEDUP = 2.5f;
    const float MOUSE_ROTATE_SPEED = 1f;
    const float HEIGHT_SCROLL_SPEED = 5f;

    string[] defaultToolSet = { "Dismantle", "Fence", "WaterWheel", "Forge", "Assembler"/*, "Miner", "WaterPump"*/};

    GameObject[] UIElements;
    public EventSystem eventSystem;
    public GameObject helperUI;
    public GameObject UIPanel;
    public GameObject detailsPanel;
    public Tools tools;
    public GameObject toolbarItemPrefab;
    public GameObject buildingDetails, tooltipPrefab;
    public Sprite frameActive, frameNotActive;
    
    public GameObject saveloadInfoPrefab;
    GameObject saveloadInfo = null;
    
    public GameObject highlight;

    GameObject currentToolbarItem;
    ITool currentTool;
    
    Vector3 lastMousePos;
    float timer = 10.0f;
    float delay = 0.3f;
    bool inUI = false;

    public const int SAVEGAME_VERSION = 6;

    void Start()
    {
        UIElements = new GameObject[defaultToolSet.Length];
        for (int i = 0; i < defaultToolSet.Length; ++i)
        {
            var go = Instantiate(toolbarItemPrefab, UIPanel.transform);
            var titem = go.GetComponent<ToolbarItem>();
            titem.setUp(tools.allTools[defaultToolSet[i]], i + 1);
            titem.name = defaultToolSet[i];
            var cur = go.transform.localPosition;
            go.transform.localPosition = new Vector3(-400 + i * 70, cur.y, cur.z);
            UIElements[i] = go;
        }
        currentTool = tools.allTools["Select"];
        currentTool.activate(highlight);
        /*var count = 0;
        for (int i = 0; i < UIPanel.transform.childCount; ++i)
            if (UIPanel.transform.GetChild(i).GetComponent<ToolbarItem>() != null)
                count++;
        count = 0;
        for (int i = 0; i < UIPanel.transform.childCount; ++i)
            if (UIPanel.transform.GetChild(i).GetComponent<ToolbarItem>() != null)
            {
                var go = UIPanel.transform.GetChild(i).gameObject;
                UIElements[count] = go;
                go.GetComponent<ToolbarItem>().updateLocation(count + 1);
                go.GetComponent<ToolbarItem>().deactivate(highlight);
                count++;
            }
        currentToolbarItem = UIElements[0];
        currentToolbarItem.GetComponent<ToolbarItem>().activate(highlight);*/
    }

    void Move(bool speedUp, Vector3 direction)
    {
        var speed = MOVE_SPEED;
        if (speedUp)
            speed *= SHIFT_SPEEDUP;
        var ang = Quaternion.Euler(new Vector3(0, camera.transform.rotation.eulerAngles.y, 0));
        var mov = ang * (direction * speed * Time.deltaTime);
        var newPos = camera.transform.parent.position + mov;
        var max = (Terrain.TERRAIN_SIZE - 1) * Terrain.SCALE;
        if (newPos.x < 0)
            newPos = new Vector3(0, newPos.y, newPos.z);
        if (newPos.z < 0)
            newPos = new Vector3(newPos.x, newPos.y, 0);
        if (newPos.x >= max)
            newPos = new Vector3(max, newPos.y, newPos.z);
        if (newPos.z >= max)
            newPos = new Vector3(newPos.x, newPos.y, max);
        var minHeight = terrain.height((int)(newPos.x / Terrain.SCALE), (int)(newPos.z / Terrain.SCALE)) + 3;
        var maxHeight = 35;
        if (newPos.y < minHeight)
            newPos = new Vector3(newPos.x, minHeight, newPos.z);
        if (newPos.y > maxHeight)
            newPos = new Vector3(newPos.x, maxHeight, newPos.z);
        camera.transform.parent.position = newPos;
    }

    void StartRotatingCam()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void StopRotatingCam()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    void RotateCam()
    {
        float h = MOUSE_ROTATE_SPEED * Input.GetAxis("Mouse X");
        float v = MOUSE_ROTATE_SPEED * Input.GetAxis("Mouse Y");
        camera.transform.parent.Rotate(0, h, 0);
        camera.transform.Rotate(-v, 0, 0);
    }

    void RaycastToTerrainHover()
    {
        currentTool.hoverOverTerrain(highlight, camera, terrain);
    }

    void RaycastToTerrain()
    {
        /*if (currentToolbarItem == null)
        {
            var ray2 = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            RaycastHit hit2;

            if (Physics.Raycast(ray2, out hit2, 200, ColliderLayers.Water))
            {
                var ix = hit2.point.x / Terrain.SCALE;
                var iy = hit2.point.z / Terrain.SCALE;
            }
            return;
        }*/
        currentTool.click(highlight, camera, terrain, Input.GetKey(KeyCode.LeftShift));
        //    currentTool.clickTerrain(highlight, terrain, hit.point);
        
        // XXX rework this part or more likely the whole function XXX
        /*if (isClick)
        {
            List<RaycastResult> res = new List<RaycastResult>();
            var ped = new PointerEventData(eventSystem);
            ped.position = Input.mousePosition;
            UIPanel.GetComponent<GraphicRaycaster>().Raycast(ped, res);
            if (res.Count > 0)
            {
                activateToolbarItem(res[0].gameObject);
                return;
            }
        }

        var ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200, 1 << 3)) {
            if (hit.transform.gameObject.GetComponent<Terrain>() == null && !isClick)
                return;

            var tree = hit.transform.gameObject.GetComponent<Tree>();
            if (tree)
            {
                if (toolSelected == ToolSelected.Select)
                    terrain.interactWithTerrain(tree.x, tree.y);
                return;
            }
            var baseG = hit.transform.gameObject.GetComponent<Base>();
            if (baseG)
            {
                if (toolSelected == ToolSelected.Select)
                    baseG.detailsPanel.SetActive(true);
                return;
            }
            var wp = hit.transform.gameObject.GetComponent<waterPump>();
            if (wp)
            {
                if (toolSelected == ToolSelected.Select)
                    wp.interact(this);
                else if (toolSelected == ToolSelected.PickedObject)
                {
                    wp.feedFuel();
                    setNullCursor();
                    return;
                }
                return;
            }

            var x = (hit.triangleIndex / 2) % (Terrain.TERRAIN_SIZE - 1);
            var y = (hit.triangleIndex / 2) / (Terrain.TERRAIN_SIZE - 1);

            if (isClick)
            {
                if (toolSelected == ToolSelected.PickedObject)
                {
                    setNullCursor();
                    terrain.spawnLog(x, y);
                }*/
                /*else if (toolSelected == ToolSelected.Miner && currentToolbarItem.GetComponent<ToolbarItem>().isLegalPlacement(highlight, terrain, hit.point))
                    terrain.spawnMiner(hit.point, highlight.transform.rotation);
                else if (toolSelected == ToolSelected.Assembler && currentToolbarItem.GetComponent<ToolbarItem>().isLegalPlacement(highlight, terrain, hit.point))
                    terrain.spawnAssembler(hit.point, highlight.transform.rotation);
                else if (toolSelected == ToolSelected.Select)
                    terrain.interactWithTerrain(x, y);*/
                //terrain.showTerrainInfo(camera, x, y);
                //else if (toolSelected == ToolSelected.Forge || toolSelected == ToolSelected.WaterWheel || toolSelected == ToolSelected.Fence)
                //    currentToolbarItem.GetComponent<ToolbarItem>().spec.behaviour.clickTerrain(highlight, terrain, hit.point);
                /*else if (toolSelected == ToolSelected.Water)
                {
                    var success = hit.transform.gameObject.GetComponent<Terrain>().spawnWaterPump(x, y);
                    if (success == null)
                        showTooltip("Too close to another pump");
                }
            }
            else
            {
                if (toolSelected == ToolSelected.Terrain)
                    hit.transform.gameObject.GetComponent<Terrain>().terrainMod(x, y, mod, val);
                //else if (toolSelected == ToolSelected.Water)
                //    hit.transform.Find("Water").GetComponent<Water>().modifyWaterSource(x, y, mod, val * Water.WATER_SOURCE_AMOUNT);
                else if (toolSelected == ToolSelected.Log)
                    hit.transform.gameObject.GetComponent<Terrain>().spawnTree(x, y);
                else if (toolSelected == ToolSelected.Grass || toolSelected == ToolSelected.Sand || toolSelected == ToolSelected.Iron)
                {
                    int kind = 0;
                    if (mod)
                        kind = 0;
                    else if (toolSelected == ToolSelected.Grass)
                        kind = 1;
                    else if (toolSelected == ToolSelected.Sand)
                        kind = 2;
                    else if (toolSelected == ToolSelected.Iron)
                        kind = 3;
                    hit.transform.gameObject.GetComponent<Terrain>().changeTerrainKind(x, y, kind);
                }
            }
        }*/
    }

    public void showBuildingMenu(GameObject building)
    {
        buildingDetails.SetActive(true);
        inUI = true;

        var sprite = tools.buildingMapping[BuildingHelper.getKind(building)].getColorIcon();
        var dets = buildingDetails.GetComponent<BuildingDetails>();
        dets.icon.sprite = sprite;
        if (building.GetComponent<Building>() != null)
            dets.populateReceipes(building.GetComponent<Building>(), terrain.items);
        else
            dets.populateConstruction(building.GetComponent<Construction>(), terrain.items);
    }

    void clickUI()
    {
        List<RaycastResult> res = new List<RaycastResult>();
        var ped = new PointerEventData(eventSystem);
        ped.position = Input.mousePosition;
        buildingDetails.GetComponent<GraphicRaycaster>().Raycast(ped, res);
        if (res.Count == 0) {
            inUI = false;
            buildingDetails.GetComponent<BuildingDetails>().deactivate();
        }
    }

    public void showTooltip(string text)
    {
        var tooltip = Object.Instantiate<GameObject>(tooltipPrefab);
        tooltip.transform.Find("Text").position = Input.mousePosition;
        tooltip.transform.Find("Text").GetComponent<TMP_Text>().text = text;
    }

    void activateToolbarItem(int index)
    {
        if (index < UIElements.Length)
            activateToolbarItem(UIElements[index]);
    }

    void activateToolbarItem(GameObject obj)
    {
        if (currentToolbarItem != null)
            currentToolbarItem.GetComponent<ToolbarItem>().deactivate(highlight, frameNotActive);
        currentToolbarItem = obj;
        var item = currentToolbarItem.GetComponent<ToolbarItem>();
        item.activate(highlight, frameActive);
        currentTool = item.tool;
    }

    void SaveGame()
    {
        terrain.gameToSave = "savegame.sav";
        saveloadInfo = Instantiate(saveloadInfoPrefab);
        saveloadInfo.transform.Find("Text").GetComponent<TMP_Text>().text = "Saving game...";
    }

    void LoadGame()
    {
        terrain.gameToLoad = "savegame.sav";
        saveloadInfo = Instantiate(saveloadInfoPrefab);
        saveloadInfo.transform.Find("Text").GetComponent<TMP_Text>().text = "Loading game...";
    }

    public void doneSaveLoad()
    {
        Destroy(saveloadInfo);
        saveloadInfo = null;
    }

    void Update()
    {
        delay -= Time.deltaTime;
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0 && helperUI.GetComponent<Text>().text.StartsWith("WSAD"))
                helperUI.GetComponent<Text>().text = "";
        }
        if (saveloadInfo != null)
            return;

        if (!inUI)
        {
            bool speedUp = false;
            if (Input.GetKey(KeyCode.LeftShift))
                speedUp = true;
            if (Input.GetKey(KeyCode.W))
                Move(speedUp, new Vector3(0, 0, 1));
            if (Input.GetKey(KeyCode.S))
                Move(speedUp, new Vector3(0, 0, -1));
            if (Input.GetKey(KeyCode.A))
                Move(speedUp, new Vector3(-1, 0, 0));
            if (Input.GetKey(KeyCode.D))
                Move(speedUp, new Vector3(1, 0, 0));

            if (Input.GetMouseButtonDown(1))
                StartRotatingCam();
            if (Input.GetMouseButtonUp(1))
                StopRotatingCam();
            if (Input.GetMouseButton(1))
                RotateCam();

            if (Input.GetKey(KeyCode.Q))
            {
                if (currentToolbarItem != null)
                    currentToolbarItem.GetComponent<ToolbarItem>().deactivate(highlight, frameNotActive);
                else
                    currentTool.deactivate(highlight);
                currentTool = tools.allTools["Select"];
                currentTool.activate(highlight);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
                activateToolbarItem(0);
            if (Input.GetKeyDown(KeyCode.Alpha2))
                activateToolbarItem(1);
            if (Input.GetKeyDown(KeyCode.Alpha3))
                activateToolbarItem(2);
            if (Input.GetKeyDown(KeyCode.Alpha4))
                activateToolbarItem(3);
            if (Input.GetKeyDown(KeyCode.Alpha5))
                activateToolbarItem(4);
            if (Input.GetKeyDown(KeyCode.Alpha6))
                activateToolbarItem(5);
            if (Input.GetKeyDown(KeyCode.Alpha7))
                activateToolbarItem(6);
            if (Input.GetKeyDown(KeyCode.Alpha8))
                activateToolbarItem(7);
            if (Input.GetKeyDown(KeyCode.Alpha9))
                activateToolbarItem(8);

            if (Input.GetMouseButtonDown(0))
                RaycastToTerrain();
            if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1))
                RaycastToTerrainHover();
            if (Input.mouseScrollDelta.y != 0)
                currentTool.rotate(highlight, Input.mouseScrollDelta.y * 20);
            if (Input.GetKey(KeyCode.Z))
                Move(false, new Vector3(0, 1, 0));
            if (Input.GetKey(KeyCode.C))
                Move(false, new Vector3(0, -1, 0));
            if (Input.GetKey(KeyCode.Escape))
                Application.Quit();
            if (Input.GetKeyDown(KeyCode.F3))
                SaveGame();
            if (Input.GetKeyDown(KeyCode.F4))
                LoadGame();
        }
        else
        {
            if (Input.GetKey(KeyCode.Q))
            {
                inUI = false;
                buildingDetails.GetComponent<BuildingDetails>().deactivate();
            }
            if (Input.GetMouseButtonDown(0))
                clickUI();
        }
    }

}
