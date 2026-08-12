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
    public GameObject buildingDetails, tooltipPrefab, intro;
    public Sprite frameActive, frameNotActive;
    
    public GameObject saveloadInfoPrefab;
    GameObject saveloadInfo = null;
    
    public GameObject highlight;

    GameObject currentToolbarItem;
    ITool currentTool;
    
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
        List<RaycastResult> res = new List<RaycastResult>();
        var ped = new PointerEventData(eventSystem);
        ped.position = Input.mousePosition;
        UIPanel.GetComponent<GraphicRaycaster>().Raycast(ped, res);
        if (res.Count > 0)
        {
            var helperText = res[0].gameObject.GetComponent<ToolbarItem>().tool.getHelperText();
            helperUI.GetComponent<TMP_Text>().text = helperText;
            highlight.SetActive(false);
            Debug.Assert(res.Count == 1);
            return;
        }
        highlight.SetActive(true);
        terrain.controls.helperUI.GetComponent<TMP_Text>().text = ""; // otherwise current tool help
        currentTool.hoverOverTerrain(highlight, camera, terrain);
    }

    void RaycastToTerrain()
    {
        List<RaycastResult> res = new List<RaycastResult>();
        var ped = new PointerEventData(eventSystem);
        ped.position = Input.mousePosition;
        UIPanel.GetComponent<GraphicRaycaster>().Raycast(ped, res);
        if (res.Count > 0)
        {
            terrain.controls.activateToolbarItem(res[0].gameObject);
            return;
        }
        currentTool.click(highlight, camera, terrain, Input.GetKey(KeyCode.LeftShift));
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

    public void activateToolbarItem(int index)
    {
        if (index < UIElements.Length)
            activateToolbarItem(UIElements[index]);
    }

    public void activateToolbarItem(GameObject obj)
    {
        if (currentToolbarItem != null)
            currentToolbarItem.GetComponent<ToolbarItem>().deactivate(highlight, frameNotActive);
        else
            currentTool.deactivate(highlight);
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

    public void closeIntro()
    {
        intro.SetActive(false);
        inUI = false;
    }

    void Update()
    {
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
            if (Input.GetKeyDown(KeyCode.F1))
            {
                intro.SetActive(true);
                inUI = true;
            }
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
