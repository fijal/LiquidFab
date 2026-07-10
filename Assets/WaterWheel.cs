using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterWheel : MonoBehaviour
{
    public GameObject gear1, gear2, gear3;
    
    // Update is called once per frame
    void Update()
    {
        gear1.transform.localRotation *= Quaternion.Euler(0, 0, 180 * Time.deltaTime);
        gear2.transform.localRotation *= Quaternion.Euler(0, 0, 180 * Time.deltaTime);
        gear3.transform.localRotation *= Quaternion.Euler(0, 0, 180 * Time.deltaTime);
    }
}
