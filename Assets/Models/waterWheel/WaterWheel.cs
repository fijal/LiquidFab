using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterWheelBehaviour : BuildingGridPlacement
{
    WaterWheelSpec wheelSpec;
    int startX, startY, lastX, lastY;
    bool placing = false;
    List<GameObject> greenChain;

    const int MAX_CHAIN_LENGTH = 6;

    public WaterWheelBehaviour(BuildingSpec spec, WaterWheelSpec wheelSpec) : base(spec)
    {
        this.wheelSpec = wheelSpec;
    }

    public override void clickTerrain(GameObject highlight, Terrain terrain, Vector3 hitPoint)
    {
        if (!placing)
        {
            placing = true;
            startX = (int)(hitPoint.x / Terrain.SCALE);
            lastX = startX;
            startY = (int)(hitPoint.z / Terrain.SCALE);
            greenChain = new List<GameObject>();
            var go = Object.Instantiate<GameObject>(wheelSpec.wheelBaseGreen, highlight.transform);
            go.transform.rotation = Quaternion.Euler(0, 90, 0);
            go.transform.position = new Vector3(startX * Terrain.SCALE, terrain.heightWater(startX, startY), startY * Terrain.SCALE);
            greenChain.Add(go);
        }
        else {
            placing = false;
            for (int i = 0; i < greenChain.Count; ++i)
            {
                terrain.spawnWaterWheel(greenChain[i].transform.position, greenChain[i].transform.rotation);
                Object.Destroy(greenChain[i]);
            }
            greenChain = null;
        }
    }

    public override void hoverOverTerrain(GameObject highlight, GameObject camera, Terrain terrain)
    {
        if (!placing)
            return;
        var ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200, 1 << 3))
        {
            var curX = (int)(hit.point.x / Terrain.SCALE);
            var curY = (int)(hit.point.z / Terrain.SCALE);

            var chainLength = (int)(Mathf.Min(MAX_CHAIN_LENGTH, Mathf.Max(1.0f, Mathf.Max(Mathf.Abs(curX - startX), Mathf.Abs(curY - startY)))));
            float chainAngle;
            if (Mathf.Abs(curX - startX) < Mathf.Abs(curY - startY))
            {
                if (curY > startY)
                    chainAngle = 0;
                else
                    chainAngle = 180;
            } else
            {
                if (curX > startX)
                    chainAngle = 90;
                else
                    chainAngle = 270;
            }
            if (chainLength > greenChain.Count)
            {
                var amount = chainLength - greenChain.Count;
                for (int i = 0; i < amount; i++)
                {
                    var go = Object.Instantiate<GameObject>(wheelSpec.wheelBaseGreen, highlight.transform);
                    greenChain.Add(go);
                }
            }
            else if (chainLength < greenChain.Count)
            {
                var amount = greenChain.Count - chainLength;
                for (int i = 0; i < amount; i++)
                {
                    var go = greenChain[greenChain.Count - 1];
                    Object.Destroy(go);
                    greenChain.RemoveAt(greenChain.Count - 1);
                }
            }
            var x = startX;
            var y = startY;
            for (int i = 0; i < greenChain.Count; i++)
            {
                var go = greenChain[i];
                go.transform.position = new Vector3(x * Terrain.SCALE, terrain.heightWater(i, startY), y * Terrain.SCALE);
                go.transform.rotation = Quaternion.Euler(0, chainAngle, 0);
                x += (int)(Mathf.Sin(Mathf.Deg2Rad * chainAngle));
                y += (int)(Mathf.Cos(Mathf.Deg2Rad * chainAngle));
            }
        }
    }
}

public class WaterWheel : MonoBehaviour
{
    public GameObject gear1, gear2, gear3;
    
    // Update is called once per frame
    void Update()
    {
        gear1.transform.localRotation *= Quaternion.Euler(0, 0, 180 * Time.deltaTime);
        gear2.transform.localRotation *= Quaternion.Euler(0, 0, 180 * Time.deltaTime);
        gear3.transform.localRotation *= Quaternion.Euler(0, 0, 180 * Time.deltaTime);
    }
}
