using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface BuildingBehaviour
{
    public void followHover(GameObject highlight, Vector3 hitPoint);
    public void rotateHighlight(GameObject highlight, float amount);
    public void clickTerrain(GameObject highlight, Terrain terrain, Vector3 hitPoint);
    public bool isLegalPlacement(GameObject highlight, Terrain terrain, Vector3 point);

    public void hoverOverTerrain(GameObject highlight, GameObject camera, Terrain terrain);
}

public class BuildingFreePlacement : BuildingBehaviour
{
    public BuildingSpec spec;

    public BuildingFreePlacement(BuildingSpec spec)
    {
        this.spec = spec;
    }

    public void followHover(GameObject highlight, Vector3 hitPoint)
    {
        highlight.transform.position = hitPoint;
    }

    public void rotateHighlight(GameObject highlight, float amount)
    {
        highlight.transform.rotation *= Quaternion.Euler(0, amount, 0);
    }

    public virtual void clickTerrain(GameObject highlight, Terrain terrain, Vector3 hitPoint)
    {
        Debug.Assert(false);
    }

    public bool isLegalPlacement(GameObject highlight, Terrain terrain, Vector3 point)
    {
        if (terrain.water.waterLevelFloat(point.x / Terrain.SCALE, point.z / Terrain.SCALE) < 0.1f)
            return false;
        var col = Physics.OverlapBox(point, spec.greenPrefab.GetComponent<BoxCollider>().size, highlight.transform.rotation, 1 << 6);
        if (col.Length > 0)
            return false;
        return true;
    }

    public void hoverOverTerrain(GameObject highlight, GameObject camera, Terrain terrain)
    {
        var ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200, 1 << 3))
        {
            if (hit.transform.gameObject.GetComponent<Terrain>() == null)
            {
                highlight.SetActive(false);
                return;
            }

            followHover(highlight, hit.point);
            if (isLegalPlacement(highlight, terrain, hit.point))
            {
                highlight.transform.Find("green").gameObject.SetActive(true);
                highlight.transform.Find("red").gameObject.SetActive(false);
            }
            else
            {
                highlight.transform.Find("green").gameObject.SetActive(false);
                highlight.transform.Find("red").gameObject.SetActive(true);
            }
            highlight.SetActive(true);
            return;
        }
        else
        {
            highlight.SetActive(false);
        }
    }
}

public class BuildingGridPlacement : BuildingBehaviour
{
    public BuildingSpec spec;

    public BuildingGridPlacement(BuildingSpec spec)
    {
        this.spec = spec;
    }

    public void followHover(GameObject highlight, Vector3 hitPoint)
    {
        var x = (int)(hitPoint.x / Terrain.SCALE);
        var y = (int)(hitPoint.z / Terrain.SCALE);
        highlight.transform.position = new Vector3(x * Terrain.SCALE, hitPoint.y, y * Terrain.SCALE);
    }

    public virtual void hoverOverTerrain(GameObject highlight, GameObject camera, Terrain terrain)
    {
        Debug.Assert(false);
    }

    public void rotateHighlight(GameObject highlight, float amount)
    {
    }

    public virtual void clickTerrain(GameObject highlight, Terrain terrain, Vector3 hitPoint)
    {
        Debug.Assert(false);
    }

    public bool isLegalPlacement(GameObject highlight, Terrain terrain, Vector3 point)
    {
        return true;
    }
}

public class BuildingSpec : MonoBehaviour
{
    public GameObject prefab;
    public bool isGridBound = false;
    public GameObject greenPrefab, redPrefab;
    public BuildingBehaviour behaviour;
    
    void Start()
    {
    }
}
