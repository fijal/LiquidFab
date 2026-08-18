using UnityEngine;

public class WallTool : ITool
{
    BuildingSpec spec;
    FenceSpec wallSpec;
    GameObject greenGhost, greenPole;
    bool placing = false;

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
            // XXX; // mark wall here
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
                highlight.transform.rotation = Quaternion.FromToRotation(new Vector3(1, 0, 0), new Vector3(hitp.x - cur.x, 0, hitp.z - cur.z));
            }
            else
            {
                terrain.followHover(highlight, hit.point);
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
