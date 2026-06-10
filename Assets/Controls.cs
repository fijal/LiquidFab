using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controls : MonoBehaviour
{
    public GameObject camera;

    const float MOVE_SPEED = 16f;
    const float SHIFT_SPEEDUP = 3f;
    const float MOUSE_ROTATE_SPEED = 1f;
    const float HEIGHT_SCROLL_SPEED = 20f;

    Vector3 lastMousePos;

    // Start is called before the first frame update
    void Start()
    {
        
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

    void RaycastToTerrain(bool mod)
    {
        var ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100))
            hit.transform.gameObject.GetComponent<Tile>().tileHit(hit.triangleIndex, mod);
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
        if (Input.GetMouseButtonDown(1))
            StartRotatingCam();
        if (Input.GetMouseButtonUp(1))
            StopRotatingCam();
        if (Input.GetMouseButton(1))
            RotateCam();
        if (Input.GetMouseButtonDown(0))
        {
            RaycastToTerrain(Input.GetKey(KeyCode.LeftShift));
        }
        if (Input.mouseScrollDelta.y != 0)
        {
            Move(false, new Vector3(0, Input.mouseScrollDelta.y * -HEIGHT_SCROLL_SPEED, 0));
        }
    
    }

}
