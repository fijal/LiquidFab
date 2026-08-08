using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FenceTool : ITool
{
    const int MAX_CHAIN_LENGTH = 15;

    BuildingSpec spec;
    FenceSpec fenceSpec;
    List<GameObject> greenChain;
    bool placing;

    public FenceTool(BuildingSpec spec, FenceSpec fenceSpec) : base() 
    {
        this.spec = spec;
        this.fenceSpec = fenceSpec;
        greenChain = new List<GameObject>();
    }

    public Sprite getGrayIcon()
    {
        return spec.grayIcon;
    }

    public Sprite getColorIcon()
    {
        return spec.colorIcon;
    }

    public GameObject getRedGhost()
    {
        return spec.redPrefab;
    }

    public void activate(GameObject highlight)
    {
        var green = Object.Instantiate<GameObject>(fenceSpec.fencePoleGreen, highlight.transform);
        green.name = "green";
        greenChain.Clear();
    }

    public void click(GameObject highlight, GameObject camera, Terrain terrain, bool modifier = false)
    {
        if (placing)
        {
            foreach (var go in greenChain)
            {
                Debug.Log(go);
                terrain.spawnBuilding(spec.prefab, go.transform.position, go.transform.rotation, spec);
            }
            deactivate(highlight);
            activate(highlight);
        }
        else
        {
            placing = true;
        }
    }

    public void deactivate(GameObject highlight)
    {
        if (highlight.transform.childCount > 0)
        {
            for (int i = 0; i < highlight.transform.childCount; i++)
                Object.Destroy(highlight.transform.GetChild(i).gameObject);
        }
        placing = false;
    }

    public void hoverOverTerrain(GameObject highlight, GameObject camera, Terrain terrain)
    {
        RaycastHit hit;

        var ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, 200, ColliderLayers.Water))
        {
            highlight.SetActive(true);
            if (placing)
            {
                var magn = (int)((hit.point - highlight.transform.position).magnitude / Terrain.SCALE);
                magn = Mathf.Min(magn, MAX_CHAIN_LENGTH);
                if (magn >= 1)
                {
                    if (magn > greenChain.Count) {
                        for (int i = 0; i < magn - greenChain.Count; i++)
                        {
                            var go = Object.Instantiate<GameObject>(spec.greenPrefab, highlight.transform);
                            go.transform.localPosition = new Vector3(greenChain.Count * Terrain.SCALE, 0, 0);
                            greenChain.Add(go);
                        }
                    } else if (magn < greenChain.Count)
                    {
                        for (int i = 0; i < greenChain.Count - magn; i++)
                        {
                            var go = greenChain[greenChain.Count - 1];
                            greenChain.RemoveAt(greenChain.Count - 1);
                            Object.Destroy(go);
                        }
                    }
                    var ang = Mathf.Atan2(hit.point.x - highlight.transform.position.x, hit.point.z - highlight.transform.position.z);
                    highlight.transform.rotation = Quaternion.Euler(0, (Mathf.Rad2Deg * ang + 270) % 360, 0);
                } else
                {

                }
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

    /*public override void spawnObject(Terrain terrain, Vector3 pos, Quaternion rot)
    {
        terrain.spawnBuilding(spec.prefab, pos, rot);
    }*/
}

public class Fence : MonoBehaviour
{
}
