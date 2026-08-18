using System.Collections.Generic;
using UnityEngine;

public class WallTool : ITool
{
    BuildingSpec spec;
    FenceSpec wallSpec;
    GameObject greenGhost, greenPole;
    bool placing = false;
    List<GameObject> chain;
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
        chain = new List<GameObject>();
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
            drawLine(wall.transform.position, lastHitPoint, terrain);
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

    void drawLine(Vector3 start, Vector3 end, Terrain terrain)
    {
        var vec = new Vector3(end.x, 0, end.z) - new Vector3(start.x, 0, start.z);
        if (vec == Vector3.zero)
            return;
        if (Mathf.Abs(vec.x) > Mathf.Abs(vec.z))
        {
            int startX, endX;
            float startY, stepY;
            if (start.x > end.x)
            {
                startX = (int)(end.x / Terrain.SCALE);
                endX = (int)(start.x / Terrain.SCALE);// + 1;
                stepY = (start.z - end.z) / (start.x - end.x);
                startY = end.z / Terrain.SCALE - stepY * (end.x / Terrain.SCALE - startX);
            } else
            {
                startX = (int)(start.x / Terrain.SCALE);
                endX = (int)(end.x / Terrain.SCALE);// + 1;
                stepY = (end.z - start.z) / (end.x - start.x);
                startY = start.z / Terrain.SCALE - stepY * (start.x / Terrain.SCALE - startX);
            }
            //fixChainLength(storage, endX - startX + 1, prefab);
            int i = 0;
            float y = startY;
            for (int x = startX; x <= endX; x++)
            {
                //storage[i].transform.position = new Vector3(x * Terrain.SCALE, terrain.heightWater(x, (int)y), ((int)y) * Terrain.SCALE);
                terrain.markWall(x, (int)y);
                i++;
                y += stepY;
            }
        } else
        {
            int startY, endY;
            float startX, stepX;
            if (start.z > end.z)
            {
                startY = (int)(end.z / Terrain.SCALE);
                endY = (int)(start.z / Terrain.SCALE);
                stepX = (start.x - end.x) / (start.z - end.z);
                startX = end.x / Terrain.SCALE - stepX * (end.z / Terrain.SCALE - startY);
            } else
            {
                startY = (int)(start.z / Terrain.SCALE);
                endY = (int)(end.z / Terrain.SCALE);
                stepX = (end.x - start.x) / (end.z - start.z);
                startX = start.x / Terrain.SCALE - stepX * (start.z / Terrain.SCALE - startY);
            }
            //fixChainLength(storage, endY - startY + 1, prefab);
            int i = 0;
            float x = startX;
            for (int y = startY; y <= endY; y++)
            {
                terrain.markWall((int)x, y);
                //storage[i].transform.position = new Vector3(((int)x) * Terrain.SCALE, terrain.heightWater((int)x, (int)y), ((int)y) * Terrain.SCALE);
                i++;
                x += stepX;
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
