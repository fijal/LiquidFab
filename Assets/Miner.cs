using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Miner : MonoBehaviour
{
    public GameObject movingPart;
    float pos = 0;
    
    void Update()
    {
        pos += Time.deltaTime;
        var c = movingPart.transform.localPosition;
        movingPart.transform.localPosition = new Vector3(c.x, 0.1f * (Mathf.Sin(pos / 3f) - 1), c.z);
    }
}
