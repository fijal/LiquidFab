using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ToolSelected
{
    Water = 1,
    Terrain = 2,
    Log = 3
}

public class Controls : MonoBehaviour
{
    public GameObject camera;

    const float MOVE_SPEED = 16f;
    const float SHIFT_SPEEDUP = 3f;
    const float MOUSE_ROTATE_SPEED = 1f;
    const float HEIGHT_SCROLL_SPEED = 5f;

    //public Sprite waterGray, waterColor, terrainGray, terrainColor;
    public GameObject[] UIElements; // in order
    public GameObject helperUI;

    GameObject currentToolbarItem;

    ToolSelected toolSelected = ToolSelected.Water;

    Vector3 lastMousePos;
    float timer = 10.0f;

    // Start is called before the first frame update
    void Start()
    {
        currentToolbarItem = UIElements[0];
    }

    void Move(bool speedUp, Vector3 direction)
    {
        var speed = MOVE_SPEED;
        if (speedUp)
            speed *= SHIFT_SPEEDUP;
        var ang = Quaternion.Euler(new Vector3(0, camera.transform.rotation.eulerAngles.y, 0));
        camera.transform.parent.position += ang * (direction * speed * Time.deltaTime);
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

    void RaycastToTerrain(bool mod, float val)
    {
        var ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100, 1 << 3))
        {
            var x = (hit.triangleIndex / 2) % (Terrain.TERRAIN_SIZE - 1);
            var y = (hit.triangleIndex / 2) / (Terrain.TERRAIN_SIZE - 1);

            if (toolSelected == ToolSelected.Terrain)
                hit.transform.gameObject.GetComponent<Terrain>().terrainMod(x, y, mod, val);
            else if (toolSelected == ToolSelected.Water)
                hit.transform.Find("Water").GetComponent<Water>().modifyWaterSource(x, y, mod, val);
            else if (toolSelected == ToolSelected.Log)
                hit.transform.gameObject.GetComponent<Terrain>().spawnLog(x, y);
        }
    }

    void activateToolbarItem(int index)
    {
        currentToolbarItem.GetComponent<ToolbarItem>().deactivate();
        currentToolbarItem = UIElements[index];
        currentToolbarItem.GetComponent<ToolbarItem>().activate();
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
        {
            activateToolbarItem(0);
            helperUI.GetComponent<Text>().text = "SHIFT to remove";
            toolSelected = ToolSelected.Water;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            activateToolbarItem(1);
            helperUI.GetComponent<Text>().text = "SHIFT to remove";
            toolSelected = ToolSelected.Terrain;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            activateToolbarItem(2);
            helperUI.GetComponent<Text>().text = "";
            toolSelected = ToolSelected.Log;
        }
        if (Input.GetMouseButtonDown(1))
            StartRotatingCam();
        if (Input.GetMouseButtonUp(1))
            StopRotatingCam();
        if (Input.GetMouseButton(1))
            RotateCam();
        if (Input.GetMouseButton(0))
        {
            RaycastToTerrain(Input.GetKey(KeyCode.LeftShift), Time.deltaTime);
        }
        if (Input.mouseScrollDelta.y != 0)
        {
            Move(false, new Vector3(0, Input.mouseScrollDelta.y * -HEIGHT_SCROLL_SPEED, 0));
        }
        if (Input.GetKey(KeyCode.Q))
            Move(false, new Vector3(0, 1, 0));
        if (Input.GetKey(KeyCode.E))
            Move(false, new Vector3(0, -1, 0));
        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0 && helperUI.GetComponent<Text>().text.StartsWith("WSAD"))
                helperUI.GetComponent<Text>().text = "";
        }
    }

}
