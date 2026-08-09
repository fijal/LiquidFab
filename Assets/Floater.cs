using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Floater : MonoBehaviour
{
    public ItemType tp;
    public Terrain terrain;

    void Start()
    {
        
    }

    private void FixedUpdate()
    {
        if (GetComponent<Rigidbody>() == null)
            return;
        var x = transform.position.x / Terrain.SCALE;
        var y = transform.position.z / Terrain.SCALE;
        var level = terrain.heightWaterFloat(x, y);
        float sizeY;
        if (GetComponent<MeshCollider>() != null)
            sizeY = 2 * GetComponent<MeshCollider>().sharedMesh.bounds.extents.y;
        else
            sizeY = GetComponent<BoxCollider>().size.y;
        var waterLevel = terrain.water.waterLevelFloat(x, y);
        if (waterLevel > sizeY && transform.position.y - level < sizeY)
        {
            GetComponent<Rigidbody>().AddForce(new Vector3(0, 12f * Mathf.Max(1f, (transform.position.y - level) / 2 / sizeY), 0));
            GetComponent<Rigidbody>().AddForce(new Vector3(0, -GetComponent<Rigidbody>().linearVelocity.y * 10f, 0));
        }
        var flowSpeed = new Vector3(terrain.water.flowXfloat(x, y), 0, terrain.water.flowYfloat(x, y));
        var vel = GetComponent<Rigidbody>().linearVelocity;
        vel = new Vector3(vel.x, 0, vel.z);
        if (waterLevel > sizeY)
        {
            GetComponent<Rigidbody>().AddForce((flowSpeed - vel * 0.7f) * 7f);
            //Debug.DrawLine(transform.position, transform.position + flowSpeed * 1f, Color.red);
            //Debug.DrawLine(transform.position, transform.position + vel * 1f, Color.green);
            //Debug.DrawLine(transform.position, transform.position + (flowSpeed - vel) * 1f, Color.red);
        }
    }

    void Update()
    {
    }
}
