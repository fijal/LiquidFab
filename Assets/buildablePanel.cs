using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class buildablePanel : MonoBehaviour
{
    public Sprite frame, frameActive;

    public void activate()
    {
        GetComponent<Image>().sprite = frameActive;
    }

    public void deactivate()
    {
        GetComponent<Image>().sprite = frame;
    }
}
