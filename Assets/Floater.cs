using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Floater : MonoBehaviour
{
    public Vector2 force;
    public Vector2 buildingForce;
    public Vector2 flowForce;
    public ItemType tp;

    void Start()
    {
        
    }

    void Update()
    {
        if (buildingForce.magnitude > 0)
        {
            transform.position += new Vector3(buildingForce.x * Time.deltaTime, 0, buildingForce.y * Time.deltaTime);
        }
        else
        {
            if (force.magnitude > 0.5f)
                force = force.normalized * 0.5f;
            transform.position += new Vector3(force.x * Time.deltaTime, 0, force.y * Time.deltaTime);
            transform.position += new Vector3(flowForce.x * Time.deltaTime, 0, flowForce.y * Time.deltaTime);
        }
    }
}
