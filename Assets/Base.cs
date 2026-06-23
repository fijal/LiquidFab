using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Base : MonoBehaviour
{
    public GameObject detailsPanel;
    public GameObject antenna;

    public void Update()
    {
        var rot = antenna.transform.rotation.eulerAngles;
        antenna.transform.rotation = Quaternion.Euler(rot.x, (rot.y + 180 * Time.deltaTime) % 360, rot.z);
    }
}
