using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToolbarItem : MonoBehaviour
{
    public Sprite gray, color, frame, frameActive;
    public string helperText;
    public ToolSelected tool;
    public bool isClick = false; // whether it's a click or contiguous

    public void activate()
    {
        GetComponent<Image>().sprite = color;
        transform.Find("Frame").GetComponent<Image>().sprite = frameActive;
    }

    public void deactivate()
    {
        GetComponent<Image>().sprite = gray;
        transform.Find("Frame").GetComponent<Image>().sprite = frame;
    }
}
