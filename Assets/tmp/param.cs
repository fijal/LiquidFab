using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class param : MonoBehaviour
{
    float f = 0;

    public GameObject target;

    // Update is called once per frame
    void FixedUpdate()
    {
        GetComponent<MeshRenderer>().material.SetFloat("_Parameter", f);
        f += 0.01f;
        f %= 3.1415f / 8;
    }
}
