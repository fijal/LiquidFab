using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToolbarItem : MonoBehaviour
{
    public Sprite gray, color, frame, frameActive;
    public GameObject greenGhost, redGhost;
    public string helperText;
    public ToolSelected tool;
    public bool isClick = false; // whether it's a click or contiguous
    public BuildingSpec spec;
    
    public void updateLocation(int location)
    {
        transform.Find("Text").GetComponent<Text>().text = location.ToString();
    }

    public void activate(GameObject highlight)
    {
        GetComponent<Image>().sprite = color;
        transform.Find("Frame").GetComponent<Image>().sprite = frameActive;

        if (greenGhost != null)
        {
            Debug.Assert(redGhost != null);
            var green = Instantiate(greenGhost, highlight.transform);
            green.name = "green";
            var red = Instantiate(redGhost, highlight.transform);
            red.name = "red";
        }
    }
    
    public bool isLegalPlacement(GameObject highlight, Terrain terrain, Vector3 point)
    {
        if (terrain.water.waterLevelFloat(point.x / Terrain.SCALE, point.z / Terrain.SCALE) < 0.1f)
            return false;
        var col = Physics.OverlapBox(point, greenGhost.GetComponent<BoxCollider>().size, highlight.transform.rotation, 1 << 6);
        if (col.Length > 0)
            return false;
        return true;
    }

    public void deactivate(GameObject highlight)
    {
        GetComponent<Image>().sprite = gray;
        transform.Find("Frame").GetComponent<Image>().sprite = frame;
        if (highlight.transform.childCount > 0)
        {
            for (int i = 0; i < highlight.transform.childCount; i++)
                Destroy(highlight.transform.GetChild(i).gameObject);
        }
    }
}
