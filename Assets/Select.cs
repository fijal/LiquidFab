using UnityEngine;

public class SelectTool : ITool
{
    GameObject selectPrefab, hover;
    GameObject selectedObject;

    public SelectTool(Select spec)
    {
        selectPrefab = spec.selectPrefab;
    }

    public void activate(GameObject highlight)
    {
    }

    public void clickTerrain(GameObject highlight, Terrain terrain, Vector3 hitPoint)
    {
        throw new System.NotImplementedException();
    }

    public void deactivate(GameObject highlight)
    {
    }

    public Sprite getColorIcon()
    {
        throw new System.NotImplementedException();
    }

    public Sprite getGrayIcon()
    {
        throw new System.NotImplementedException();
    }

    public GameObject getRedGhost()
    {
        throw new System.NotImplementedException();
    }

    public void hoverOverTerrain(GameObject highlight, GameObject camera, Terrain terrain)
    {
        var ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200, ColliderLayers.Floaters))
        {
            var go = hit.transform.gameObject;
            if (go == selectedObject)
            {
                return;
            }
            if (hover != null)
                Object.Destroy(hover);
            hover = Object.Instantiate<GameObject>(selectPrefab, go.transform);
            hover.transform.position = go.transform.position;
            hover.transform.rotation = Quaternion.Euler(90, 0, 0);
            selectedObject = go;
        } else
        {
            Object.Destroy(hover);
            selectedObject = null;
            hover = null;
        }
    }

    public void rotate(GameObject highlight, float amount)
    {
    }
}

public class Select : MonoBehaviour
{
    public GameObject selectPrefab;
}
