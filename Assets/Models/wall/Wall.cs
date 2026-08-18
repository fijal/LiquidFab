using System.Collections.Generic;
using UnityEngine;

public class WallTool : ITool
{
    BuildingSpec spec;
    FenceSpec wallSpec;
    GameObject greenGhost, greenPole;
    bool placing = false;
    Vector3 lastHitPoint;

    public WallTool(BuildingSpec spec, FenceSpec fenceSpec) : base()
    {
        this.spec = spec;
        this.wallSpec = fenceSpec;
    }

    public void activate(GameObject highlight)
    {
        greenPole = Object.Instantiate<GameObject>(wallSpec.fencePoleGreen, highlight.transform);
        //green.transform.localPosition = new Vector3(0, -1, 0);
        greenPole.name = "green";
        placing = false;
    }

    public void click(GameObject highlight, GameObject camera, Terrain terrain, bool modifier = false)
    {
        if (!placing)
        {
            placing = true;
            Object.Destroy(highlight.transform.Find("green").gameObject);
            greenGhost = Object.Instantiate<GameObject>(spec.greenPrefab, highlight.transform);
        } else
        {
            var wall = terrain.spawnBuilding(spec.prefab, greenGhost.transform.position, greenGhost.transform.rotation, spec);
            wall.transform.localScale = greenGhost.transform.localScale;
            terrain.drawWallLine(wall.transform.position, lastHitPoint);
            Object.Destroy(greenGhost);
            activate(highlight);
        }
    }

    public void deactivate(GameObject highlight)
    {
        if (greenGhost != null)
        {
            Object.Destroy(greenGhost);
            greenGhost = null;
        }
        if (greenPole != null)
        {
            Object.Destroy(greenPole);
            greenPole = null;
        }
    }

    public string getHelperText()
    {
        return "Wall";
    }

    public BuildingSpec getSpec()
    {
        return spec;
    }

    void fixChainLength(List<GameObject> chain, int count, GameObject prefab)
    {
        if (chain.Count < count)
        {
            var c = count - chain.Count;
            for (int i = 0; i < c; i++)
                chain.Add(Object.Instantiate<GameObject>(prefab));
        } else if (chain.Count > count)
        {
            var c = chain.Count - count;
            for (int i = 0; i < c; i++)
            {
                var go = chain[chain.Count - 1];
                chain.RemoveAt(chain.Count - 1);
                Object.Destroy(go);
            }
        }
    }

    public void hoverOverTerrain(GameObject highlight, GameObject camera, Terrain terrain)
    {
        RaycastHit hit;

        var ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, ColliderLayers.Depth, ColliderLayers.Water))
        {
            highlight.SetActive(true);
            if (placing)
            {
                var cur = greenGhost.transform.position;
                var hitp = hit.point;
                var newx = (new Vector3(hitp.x, 0, hitp.z) - new Vector3(cur.x, 0, cur.z)).magnitude * 2;
                var curS = greenGhost.transform.localScale;
                greenGhost.transform.localScale = new Vector3(newx, curS.y, curS.z);
                lastHitPoint = hit.point;
                highlight.transform.rotation = Quaternion.FromToRotation(new Vector3(1, 0, 0), new Vector3(hitp.x - cur.x, 0, hitp.z - cur.z));
            }
            else
            {
                //var col = greenGhost.GetComponent<CapsuleCollider>();
                //col.Rayc
                var c = Physics.OverlapSphere(hit.point, 0.4f, ColliderLayers.AllBuildings);
                if (c.Length > 0)
                {
                    var snapPoint = BuildingHelper.findClosestSnapPoint(hit.point, c[0].GetComponent<Building>().snapPoints);
                    terrain.followHover(highlight, snapPoint);
                    //Debug.Log("hit something");
                }
                else
                {
                    terrain.followHover(highlight, hit.point);
                }
            }
        }
        else
        {
            highlight.SetActive(false);
        }
    }

    public void rotate(GameObject highlight, float amount)
    {
    }
}

public class Wall : MonoBehaviour
{
}
