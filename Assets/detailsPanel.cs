using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Buildables
{
    Pump = 0
}

public class detailsPanel : MonoBehaviour
{
    public GameObject pumpPanel, mainBuildPanel, buildingPanel;

    public void close()
    {
        gameObject.SetActive(false);
    }

    public void select(int no)
    {
        pumpPanel.GetComponent<buildablePanel>().activate();
    }
}
