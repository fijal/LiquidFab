using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToolbarItem : MonoBehaviour
{
    public ITool tool;
    
    public void updateLocation(int location)
    {
        transform.Find("Text").GetComponent<Text>().text = location.ToString();
    }

    public void activate(GameObject highlight, Sprite frameActive)
    {
        GetComponent<Image>().sprite = tool.getSpec().colorIcon;
        transform.Find("Frame").GetComponent<Image>().sprite = frameActive;
        tool.activate(highlight);
    }

    public void setUp(ITool tool, int pos)
    {
        this.tool = tool;
        GetComponent<Image>().sprite = tool.getSpec().grayIcon;
        updateLocation(pos);
    }

    public void deactivate(GameObject highlight, Sprite frameNotActive)
    {
        GetComponent<Image>().sprite = tool.getSpec().grayIcon;
        transform.Find("Frame").GetComponent<Image>().sprite = frameNotActive;
        tool.deactivate(highlight);
    }
}
