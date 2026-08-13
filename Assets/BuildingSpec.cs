using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BuildingFreePlacement :  ITool
{
    public BuildingSpec spec;

    public BuildingSpec getSpec()
    {
        return spec;
    }

    public string getHelperText()
    {
        return spec.helperText;
    }

    public void activate(GameObject highlight)
    {
        var green = Object.Instantiate<GameObject>(spec.greenPrefab, highlight.transform);
        green.name = "green";
        var red = Object.Instantiate<GameObject>(spec.redPrefab, highlight.transform);
        red.name = "red";
    }

    public void deactivate(GameObject highlight)
    {
        if (highlight.transform.childCount > 0)
        {
            for (int i = 0; i < highlight.transform.childCount; i++)
                Object.Destroy(highlight.transform.GetChild(i).gameObject);
        }
    }

    public BuildingFreePlacement(BuildingSpec spec)
    {
        this.spec = spec;
    }

    public void rotate(GameObject highlight, float amount)
    {
        highlight.transform.rotation *= Quaternion.Euler(0, amount, 0);
    }

    public void click(GameObject highlight, GameObject camera, Terrain terrain, bool modifier=false)
    {
        var ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200, ColliderLayers.Water))
        {
            if (isLegalPlacement(highlight, terrain, hit.point))
            {
                if (modifier)
                    placeBuilding(highlight, terrain, hit.point);
                else
                    placeConstruction(highlight, terrain, hit.point);
            }
        }
    }

    public virtual void placeBuilding(GameObject highlight, Terrain terrain, Vector3 hitPoint)
    {
        terrain.spawnBuilding(spec.prefab, hitPoint, highlight.transform.rotation, spec);
    }

    public void placeConstruction(GameObject highlight, Terrain terrain, Vector3 hitPoint)
    {
        terrain.spawnBuilding(spec.whitePrefab, hitPoint, highlight.transform.rotation, spec);
    }

    public virtual bool isLegalPlacement(GameObject highlight, Terrain terrain, Vector3 point)
    {
        if (terrain.water.waterLevelFloat(point.x / Terrain.SCALE, point.z / Terrain.SCALE) < 0.1f)
            return false;
        var col = Physics.OverlapBox(point, spec.greenPrefab.GetComponent<BoxCollider>().size, highlight.transform.rotation,
                                     ColliderLayers.Buildings | ColliderLayers.BuildingsNoFloater);
        if (col.Length > 0)
            return false;
        return true;
    }

    public void hoverOverTerrain(GameObject highlight, GameObject camera, Terrain terrain)
    {
        var ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200, ColliderLayers.Water))
        {
            Debug.Assert(hit.transform.gameObject.GetComponent<Water>() != null);
            terrain.followHover(highlight, hit.point);
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
        }
        else
        {
            highlight.SetActive(false);
        }
    }
}

public class BuildingGridPlacement : ITool
{
    public BuildingSpec spec;
    int startX, startY;
    bool placing = false;
    List<GameObject> greenChain;

    public BuildingSpec getSpec()
    {
        return spec;
    }

    public string getHelperText()
    {
        return spec.helperText;
    }

    public virtual int GetMaxChainLength()
    {
        Debug.Assert(false);
        return -1;
    }

    public BuildingGridPlacement(BuildingSpec spec)
    {
        this.spec = spec;
    }

    public void activate(GameObject highlight)
    {

    }

    public void deactivate(GameObject highlight)
    {
        placing = false;
        if (highlight.transform.childCount > 0)
        {
            for (int i = 0; i < highlight.transform.childCount; i++)
                Object.Destroy(highlight.transform.GetChild(i).gameObject);
        }
    }

    public void hoverOverTerrain(GameObject highlight, GameObject camera, Terrain terrain)
    {
        if (!placing)
            return;
        var ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200, ColliderLayers.Water))
        {
            var curX = (int)(hit.point.x / Terrain.SCALE);
            var curY = (int)(hit.point.z / Terrain.SCALE);

            var chainLength = (int)(Mathf.Min(GetMaxChainLength(), Mathf.Max(1.0f, Mathf.Max(Mathf.Abs(curX - startX), Mathf.Abs(curY - startY)))));
            float chainAngle;
            if (Mathf.Abs(curX - startX) < Mathf.Abs(curY - startY))
            {
                if (curY > startY)
                    chainAngle = 0;
                else
                    chainAngle = 180;
            }
            else
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
                    var go = Object.Instantiate<GameObject>(spec.greenPrefab, highlight.transform);
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
                go.transform.rotation = Quaternion.Euler(0, (chainAngle + 180) % 360, 0);
                x += (int)(Mathf.Sin(Mathf.Deg2Rad * chainAngle));
                y += (int)(Mathf.Cos(Mathf.Deg2Rad * chainAngle));
            }
        }
    }

    public void rotate(GameObject highlight, float amount)
    {
    }

    public void click(GameObject highlight, GameObject camera, Terrain terrain, bool modifier = false)
    {
        var ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200, ColliderLayers.Water))
        {
            // XXX sure
            if (!placing)
            {
                placing = true;
                startX = (int)(hit.point.x / Terrain.SCALE);
                startY = (int)(hit.point.z / Terrain.SCALE);
                greenChain = new List<GameObject>();
                var go = Object.Instantiate<GameObject>(spec.greenPrefab, highlight.transform);
                go.transform.rotation = Quaternion.Euler(0, 90, 0);
                go.transform.position = new Vector3(startX * Terrain.SCALE, terrain.heightWater(startX, startY), startY * Terrain.SCALE);
                greenChain.Add(go);
            }
            else
            {
                placing = false;
                for (int i = 0; i < greenChain.Count; ++i)
                {
                    spawnObject(terrain, greenChain[i].transform.position, greenChain[i].transform.rotation);
                    Object.Destroy(greenChain[i]);
                }
                greenChain = null;
            }
        }
    }

    public virtual void spawnObject(Terrain terrain, Vector3 pos, Quaternion rot)
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
    public GameObject greenPrefab, redPrefab, whitePrefab;
    public Sprite colorIcon, grayIcon;
    public string helperText;
    
    public Dictionary<ItemType, int> buildingCost;
    [HideInInspector] public Receipe[] receipes;
}
