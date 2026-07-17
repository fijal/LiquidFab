using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DismantleTool : ITool
{
    Dismantle spec;
    GameObject currentObject, currentGhost;

    public DismantleTool(Dismantle spec)
    {
        this.spec = spec;
    }

    public GameObject getRedGhost()
    {
        throw new System.NotImplementedException();
    }

    public void activate(GameObject highlight)
    {
    }

    public void clickTerrain(GameObject highlight, Terrain terrain, Vector3 hitPoint)
    {
        if (currentObject == null)
            return;
        Object.Destroy(currentGhost);
        terrain.removeBuilding(currentObject);
        currentObject = null;
    }

    public void deactivate(GameObject highlight)
    {
        if (currentObject != null)
        {
            reenableObject(currentObject);
            Object.Destroy(currentGhost);
            currentObject = null;
        }
    }

    public Sprite getColorIcon()
    {
        return spec.dismantleColor;
    }

    public Sprite getGrayIcon()
    {
        return spec.dismantleGray;
    }

    void recursivelyDisableMeshRendering(GameObject cur)
    {
        // A bit of a hack, can't think about a better plan
        if (cur.GetComponent<MeshRenderer>())
        {
            cur.GetComponent<MeshRenderer>().enabled = false;
        }

        for (int i = 0; i < cur.transform.childCount; ++i)
        {
            var c = cur.transform.GetChild(i).gameObject;
            recursivelyDisableMeshRendering(c);
        }
    }

    void reenableObject(GameObject cur)
    {
        if (cur.GetComponent<MeshRenderer>())
        {
            cur.GetComponent<MeshRenderer>().enabled = true;
        }

        for (int i = 0; i < cur.transform.childCount; ++i)
        {
            var c = cur.transform.GetChild(i).gameObject;
            reenableObject(c);
        }
    }

    public void hoverOverTerrain(GameObject highlight, GameObject camera, Terrain terrain)
    {
        var ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200, 1 << 6))
        {
            if (currentObject == hit.transform.gameObject)
                return;
            if (currentObject != null)
            {
                reenableObject(currentObject);
                Object.Destroy(currentGhost);
            }
            currentObject = hit.transform.gameObject;
            recursivelyDisableMeshRendering(currentObject);
            var red = terrain.controls.tools.buildingMapping[currentObject.GetComponent<Building>().kind].getRedGhost();
            currentGhost = Object.Instantiate<GameObject>(red, currentObject.transform);
        } else if (currentObject != null)
        {
            reenableObject(currentObject);
            Object.Destroy(currentGhost);
            currentObject = null;
        }
    }

    public void rotate(GameObject highlight, float amount)
    {
    }
}

public class Dismantle : MonoBehaviour
{
    public Sprite dismantleColor, dismantleGray;
    public Material transparentRed;
}
