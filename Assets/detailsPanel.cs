using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DetailsPanel : MonoBehaviour
{
    public Controls controls;
    public GameObject interactable;

    public Transform fuelLevel;
    
    public void close()
    {
        Destroy(gameObject);
        controls.detailsPanelActive = false;
    }

    public void Start()
    {
    }

    public void FixedUpdate()
    {
        var wp = interactable.GetComponent<waterPump>();
        fuelLevel.localScale = new Vector3(wp.fuelLevel / waterPump.MAX_FUEL, 1, 1);
    }
}
