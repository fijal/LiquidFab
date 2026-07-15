using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DetailsPanel : MonoBehaviour
{
    public Controls controls;
    public GameObject interactable;

    public Transform fuelLevel;
    public TMP_Text logCount;
    
    public void close()
    {
        Destroy(gameObject);
        controls.inOverlay = false;
    }

    public void Start()
    {
    }

    public void FixedUpdate()
    {
        var wp = interactable.GetComponent<waterPump>();
        fuelLevel.localScale = new Vector3(wp.fuelLevel / waterPump.MAX_FUEL, 1, 1);
        logCount.text = $"{wp.logs}/5";
    }
}
