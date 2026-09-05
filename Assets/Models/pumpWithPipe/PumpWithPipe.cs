using UnityEngine;

class PumpWithPipeTool : ITool
{
    bool placing = false;
    BuildingSpec spec;
    PumpWithPipeSpec pipeSpec;
    GameObject pipeSegment;
    Vector3 hitp;

    public PumpWithPipeTool(BuildingSpec spec, PumpWithPipeSpec pipeSpec) : base()
    {
        this.spec = spec;
        this.pipeSpec = pipeSpec;
    }

    public void activate(GameObject highlight)
    {
        var green = Object.Instantiate<GameObject>(spec.greenPrefab, highlight.transform);
        green.name = "green";
    }

    public void click(GameObject highlight, GameObject camera, Terrain terrain, bool modifier = false)
    {
        if (!placing)
        {
            placing = true;
            pipeSegment = Object.Instantiate<GameObject>(pipeSpec.pipeSegmentGreen, highlight.transform);
            pipeSegment.transform.localPosition = new Vector3(0, 0.3f, 0);
        } else
        {
            var pump = terrain.spawnBuilding(spec.prefab, highlight.transform.position, highlight.transform.rotation, spec);
            var pipe = Object.Instantiate<GameObject>(pipeSpec.pipeSegment, pump.transform);
            pipe.transform.localPosition = new Vector3(0, 0.3f, 0);
            pipe.transform.localScale = pipeSegment.transform.localScale;
            pipe.transform.rotation = pipeSegment.transform.rotation;
            var pp = pump.GetComponent<PumpWithPipe>();
            pp.outX = hitp.x;
            pp.outY = hitp.z;
            deactivate(highlight);
            activate(highlight);
            placing = false;
        }
    }

    public void deactivate(GameObject highlight)
    {
        if (highlight.transform.childCount > 0)
        {
            for (int i = 0; i < highlight.transform.childCount; i++)
                Object.Destroy(highlight.transform.GetChild(i).gameObject);
        }
    }

    public string getHelperText()
    {
        return "Pump with pipe";
    }

    public BuildingSpec getSpec()
    {
        return spec;
    }

    public void hoverOverTerrain(GameObject highlight, GameObject camera, Terrain terrain)
    {
        var ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, ColliderLayers.Depth, ColliderLayers.Water))
        {
            highlight.SetActive(true);
            if (!placing)
            {
                terrain.followHover(highlight, hit.point);
            } else
            {
                var sizeX = (hit.point + new Vector3(0, 0.3f, 0) - highlight.transform.position).magnitude * 2;
                pipeSegment.transform.localScale = new Vector3(sizeX, 1, 1);
                var cur = highlight.transform.position;
                pipeSegment.transform.localRotation = Quaternion.FromToRotation(new Vector3(1, 0, 0), new Vector3(hit.point.x - cur.x, hit.point.y - cur.y, hit.point.z - cur.z));
                hitp = hit.point;
            }
        } else
        {
            highlight.SetActive(false);
        }
    }

    public void rotate(GameObject highlight, float amount)
    {
    }
}

public class PumpWithPipe : MonoBehaviour
{
    public float outX, outY;
}
