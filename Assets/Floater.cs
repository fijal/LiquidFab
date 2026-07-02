using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Floater : MonoBehaviour
{
    public Vector2 force;

    void Start()
    {
        
    }

    void Update()
    {
        if (force.magnitude > 0.5f)
            force = force.normalized * 0.5f;
        transform.position += new Vector3(force.x * Time.deltaTime, 0, force.y * Time.deltaTime);
    }
}
