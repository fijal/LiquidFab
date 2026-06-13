using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToolbarItem : MonoBehaviour
{
    public Sprite gray, color;

    public void activate()
    {
        GetComponent<Image>().sprite = color;
    }

    public void deactivate()
    {
        GetComponent<Image>().sprite = gray;
    }
}
