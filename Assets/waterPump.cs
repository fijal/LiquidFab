using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class waterPump : MonoBehaviour
{
    float pos = 0;
    public float basePos = 0;
    // Update is called once per frame
    
    void FixedUpdate()
    {
        pos += Time.fixedDeltaTime;
        var c = transform.position;
        transform.position = new Vector3(c.x, basePos + 0.05f * (Mathf.Sin(pos * 3f) - 1), c.z);
    }

    public void interact(Controls controls)
    {
        Instantiate(controls.detailsPanel);
    }
}
