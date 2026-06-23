using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ToolSelected
{
    Select = 0,
    Water = 1,
    Terrain = 2,
    Log = 3,
    Magnet = 4,
    Miner = 5,
    Grass = 6,
    Sand = 7,
    Iron = 8
}

public class Controls : MonoBehaviour
{
    public new GameObject camera;
    public Terrain terrain;

    const float MOVE_SPEED = 10f;
    const float SHIFT_SPEEDUP = 2.5f;
    const float MOUSE_ROTATE_SPEED = 1f;
    const float HEIGHT_SCROLL_SPEED = 5f;

    GameObject[] UIElements;
    public EventSystem eventSystem;
    public GameObject helperUI;
    public GameObject UIPanel;
    public GameObject tooltip;

    public Texture2D mouseCursorLog;

    GameObject currentToolbarItem;
    ToolSelected toolSelected = ToolSelected.Select;

    Vector3 lastMousePos;
    float timer = 10.0f;

    void Start()
    {
        var count = 0;
        for (int i = 0; i < UIPanel.transform.childCount; ++i)
            if (UIPanel.transform.GetChild(i).GetComponent<ToolbarItem>() != null)
                count++;
        UIElements = new GameObject[count];
        count = 0;
        for (int i = 0; i < UIPanel.transform.childCount; ++i)
            if (UIPanel.transform.GetChild(i).GetComponent<ToolbarItem>() != null)
            {
                var go = UIPanel.transform.GetChild(i).gameObject;
                UIElements[count] = go;
                go.GetComponent<ToolbarItem>().updateLocation(count + 1);
                go.GetComponent<ToolbarItem>().deactivate();
                count++;
            }
        currentToolbarItem = UIElements[0];
        currentToolbarItem.GetComponent<ToolbarItem>().activate();
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

    void RaycastToTerrain(bool isClick, bool mod=false, float val=0)
    {
        // bleh, if we are over the UI, ignore the button
        List<RaycastResult> res = new List<RaycastResult>();
        var ped = new PointerEventData(eventSystem);
        ped.position = Input.mousePosition;
        UIPanel.GetComponent<GraphicRaycaster>().Raycast(ped, res);
        if (res.Count > 0)
        {
            activateToolbarItem(res[0].gameObject);
            return;
        }

        var ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200, 1 << 3))
        {
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
                baseG.detailsPanel.SetActive(true);
                return;
            }

            var x = (hit.triangleIndex / 2) % (Terrain.TERRAIN_SIZE - 1);
            var y = (hit.triangleIndex / 2) / (Terrain.TERRAIN_SIZE - 1);

            if (isClick)
            {
                if (toolSelected == ToolSelected.Miner)
                    hit.transform.gameObject.GetComponent<Terrain>().spawnMiner(x, y);
                else if (toolSelected == ToolSelected.Select)
                    terrain.interactWithTerrain(x, y);
                //terrain.showTerrainInfo(camera, x, y);
                else if (toolSelected == ToolSelected.Magnet)
                    hit.transform.gameObject.GetComponent<Terrain>().spawnMagnet(x, y);
                else if (toolSelected == ToolSelected.Water)
                {
                    var success = hit.transform.gameObject.GetComponent<Terrain>().spawnWaterPump(x, y);
                    if (!success)
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
        }
    }

    void RaycastToTerrainClick()
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
        
        var ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (!currentToolbarItem.GetComponent<ToolbarItem>().isClick)
            return;
        
        if (Physics.Raycast(ray, out hit, 200, 1 << 3))
        {
            var x = (hit.triangleIndex / 2) % (Terrain.TERRAIN_SIZE - 1);
            var y = (hit.triangleIndex / 2) / (Terrain.TERRAIN_SIZE - 1);
            if (toolSelected == ToolSelected.Miner)
                hit.transform.gameObject.GetComponent<Terrain>().spawnMiner(x, y);
            else if (toolSelected == ToolSelected.Select)
                terrain.interactWithTerrain(x, y);
                //terrain.showTerrainInfo(camera, x, y);
            else if (toolSelected == ToolSelected.Magnet)
                hit.transform.gameObject.GetComponent<Terrain>().spawnMagnet(x, y);
            else if (toolSelected == ToolSelected.Water)
            {
                var success = hit.transform.gameObject.GetComponent<Terrain>().spawnWaterPump(x, y);
                if (!success)
                    showTooltip("Too close to another pump");
            }
        }
    }

    public void showTooltip(string text)
    {
        tooltip.GetComponent<TMP_Text>().text = text;
        var rt = tooltip.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y + 30);
        tooltip.SetActive(true);
        tooltip.GetComponent<Tooltip>().fadeTimer = 0.0f;
        tooltip.GetComponent<TMP_Text>().color = new Color(1, 1, 1, 1);
    }

    void activateToolbarItem(int index)
    {
        activateToolbarItem(UIElements[index]);
    }

    void activateToolbarItem(GameObject obj)
    {
        currentToolbarItem.GetComponent<ToolbarItem>().deactivate();
        currentToolbarItem = obj;
        var item = currentToolbarItem.GetComponent<ToolbarItem>();
        item.activate();
        helperUI.GetComponent<Text>().text = item.helperText;
        toolSelected = item.tool;
    }

    public void changeMouseCursorToLog()
    {
        Cursor.SetCursor(mouseCursorLog, Vector2.zero, CursorMode.Auto);
    }

    void SaveGame()
    {

    }

    void LoadGame()
    {
        //terrain.gameToLoad = true;
    }

    void Update()
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

        if (Input.GetMouseButtonDown(1))
            StartRotatingCam();
        if (Input.GetMouseButtonUp(1))
            StopRotatingCam();
        if (Input.GetMouseButton(1))
            RotateCam();

        if (Input.GetMouseButtonDown(0))
            RaycastToTerrain(true);
        if (Input.GetMouseButton(0))
            RaycastToTerrain(false, Input.GetKey(KeyCode.LeftShift), Time.deltaTime);
        if (Input.mouseScrollDelta.y != 0)
        {
            //Move(false, new Vector3(0, Input.mouseScrollDelta.y * -HEIGHT_SCROLL_SPEED, 0));
        }
        if (Input.GetKey(KeyCode.Q))
            Move(false, new Vector3(0, 1, 0));
        if (Input.GetKey(KeyCode.E))
            Move(false, new Vector3(0, -1, 0));
        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();
        if (Input.GetKey(KeyCode.F3))
            SaveGame();
        if (Input.GetKey(KeyCode.F4))
            LoadGame();
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0 && helperUI.GetComponent<Text>().text.StartsWith("WSAD"))
                helperUI.GetComponent<Text>().text = "";
        }
    }

}
